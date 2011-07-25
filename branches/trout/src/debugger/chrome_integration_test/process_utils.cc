// Copyright (c) 2011 The Native Client Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.
#include "debugger/chrome_integration_test/process_utils.h"
#include <tlhelp32.h>
#include <algorithm>

namespace {
const int kWaitForKilledProcessMs = 1000;
std::deque<int> glb_preexisting_pids;

bool IsPreexistingProcess(int pid) {
  return (glb_preexisting_pids.end() !=
      std::find(glb_preexisting_pids.begin(), glb_preexisting_pids.end(), pid));
}
}  // namespace

void process_utils::CreateListOfPreexistingProcesses() {
  std::deque<std::pair<int, int> > procs;
  GetProcessList(&procs);
  for (size_t i = 0; i < procs.size(); i++)
    glb_preexisting_pids.push_back(procs[i].first);
}

HANDLE process_utils::StartProcess(const std::string& command_line,
                                   const std::string& dir) {
  STARTUPINFO si;
  memset(&si, 0, sizeof(si));
  si.cb = sizeof(si);
  PROCESS_INFORMATION pi;
  memset(&pi, 0, sizeof(pi));

  char* cmd_dup = _strdup(command_line.c_str());
  if (NULL == cmd_dup)
    return 0;

  BOOL res = ::CreateProcess(NULL,
                             cmd_dup,
                             NULL,
                             NULL,
                             FALSE,
                             CREATE_NEW_CONSOLE,
                             NULL,
                             dir.size() > 0 ? dir.c_str() : NULL,
                             &si,
                             &pi);
  free(cmd_dup);
  if (!res)
    return 0;

  ::CloseHandle(pi.hThread);
  return pi.hProcess;
}

void process_utils::GetProcessList(std::deque<std::pair<int, int> >* procs) {
  PROCESSENTRY32 pe;
  memset(&pe, 0, sizeof(PROCESSENTRY32));
  pe.dwSize = sizeof(PROCESSENTRY32);
  HANDLE h_snap = ::CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
  if (INVALID_HANDLE_VALUE == h_snap)
    return;
  if (::Process32First(h_snap, &pe)) {
    do {
      procs->push_back(std::pair<int, int>(pe.th32ProcessID,
                                           pe.th32ParentProcessID));
    } while (::Process32Next(h_snap, &pe));
  }
  ::CloseHandle(h_snap);
}

void process_utils::GetChildrenPIDs(HANDLE h_proc, std::deque<int>* pids) {
  std::deque<std::pair<int, int>> procs;
  GetProcessList(&procs);
  int pid = GetProcessId(h_proc);
  pids->push_back(pid);
  bool added = false;
  do {
    added = false;
    std::deque<std::pair<int, int> >::iterator it = procs.begin();
    while (procs.end() != it) {
      if (pids->end() != std::find(pids->begin(), pids->end(), it->second)) {
        int pid = it->first;
        // If process was there before we created our own, it
        // is probably not our child.
        if (!IsPreexistingProcess(pid))
          pids->push_back(pid);
        procs.erase(it);
        added = true;
        break;
      }
      it++;
    }
  } while (added);
  pids->pop_front();  // remove parent
}

void process_utils::KillProcessTree(HANDLE h_proc) {
  std::deque<int> children_pids;
  process_utils::GetChildrenPIDs(h_proc, &children_pids);
  for (size_t i = 0; i < children_pids.size(); i++) {
    int pid = children_pids[i];
    HANDLE h_proc = ::OpenProcess(PROCESS_TERMINATE, FALSE, pid);
    if (INVALID_HANDLE_VALUE != h_proc) {
      ::TerminateProcess(h_proc, 0);
      ::WaitForSingleObject(h_proc, kWaitForKilledProcessMs);
      ::CloseHandle(h_proc);
    }
  }
  ::TerminateProcess(h_proc, 0);
}


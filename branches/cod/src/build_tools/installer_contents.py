#!/usr/bin/python
# Copyright (c) 2011 The Native Client Authors. All rights reserved.
# Use of this source code is governed by a BSD-style license that can be
# found in the LICENSE file.

"""White-list of directories and files that get installed in the SDK.

Note that the list of directories is kept separate from the list of files
because copying a directory is treated differently than copying files on
Windows.
"""

import os
import sys
from nacl_sdk_scons import nacl_utils


# For each path in this list, its entire contents are added to the SDK
# installer.  Directories are denoted by a trailing '/' - this is important
# because they have to be treated separately on Windows for plain files (the
# copy mechanism is different).  Note that the path separator is hard-coded to
# be '/'.  When accessing these lists, use the Get*() functions, which replace
# the '/' with the correct platform-specific path separator as defined by
# os.path.
INSTALLER_CONTENTS = [
    'build_tools/nacl_sdk_scons/make_nacl_env.py',
    'build_tools/nacl_sdk_scons/nacl_utils.py',
    'build_tools/nacl_sdk_scons/site_tools/',
    'examples/build.scons',
    'examples/favicon.ico',
    'examples/geturl/',
    'examples/hello_world/',
    'examples/hello_world_c/',
    'examples/httpd.py',
    'examples/index.html',
    'examples/input_events/',
    'examples/load_progress/',
    'examples/pi_generator/',
    'examples/pong/',
    'examples/scons',
    'examples/sine_synth/',
    'examples/tumbler/',
    'project_templates/README',
    'project_templates/c/',
    'project_templates/cc/',
    'project_templates/html/',
    'project_templates/init_project.py',
    'project_templates/scons',
    'third_party/scons-2.0.1/',
]

INSTALLER_CONTENTS.append('%s/' % nacl_utils.ToolchainPath(base_dir='',
                                                           variant='newlib'))
INSTALLER_CONTENTS.append('%s/' % nacl_utils.ToolchainPath(base_dir='',
                                                           variant='glibc'))

LINUX_ONLY_CONTENTS = [
    'third_party/ppapi/',
    'third_party/valgrind/memcheck.sh',
    'third_party/valgrind/tsan.sh',
    'third_party/valgrind/nacl.supp',
    'third_party/valgrind/nacl.ignore',
    'third_party/valgrind/README',
    'third_party/valgrind/bin/memcheck',
    'third_party/valgrind/bin/tsan',
]

MAC_ONLY_CONTENTS = [
    'third_party/ppapi/',
]

WINDOWS_ONLY_CONTENTS = [
    'examples/httpd.cmd',
    'examples/scons.bat',
    'project_templates/scons.bat',
    'debugger/nacl-gdb_server/x64/Debug/'
]

# These files are user-readable documentation files, and as such get some
# further processing on Windows (\r\n line-endings and .txt file suffix).
# On non-Windows platforms, the installer content list is the union of
# |INSTALLER_CONTENTS| and |DOCUMENTATION_FILES|.
DOCUMENTATION_FILES = [
    'AUTHORS',
    'COPYING',
    'LICENSE',
    'NOTICE',
    'README',
]


def GetToolchainManifest(toolchain):
  '''Get the toolchain manifest file.

  These manifest files are used to create NSIS file sections for the
  toolchains.  The manifest files are considered the source of truth for
  symbolic links and other filesystem-specific information that is not
  discoverable using python in Windows.

  Args:
    toolchain: The toolchain variant.  Currently supported values are 'newlib'
        and 'glibc'.

  Returns:
    The os-specific path to the toolchain manifest file, relative to the SDK's
        src directory.
  '''
  WINDOWS_TOOLCHAIN_MANIFESTS = {
      'newlib': 'naclsdk_win_x86.tgz.manifest',
      'glibc': 'toolchain_win_x86.tar.xz.manifest',
  }
  MAC_TOOLCHAIN_MANIFESTS = {
      'newlib': 'naclsdk_mac_x86.tgz.manifest',
      'glibc': 'toolchain_mac_x86.tar.bz2.manifest',
  }
  LINUX_TOOLCHAIN_MANIFESTS = {
      'newlib': 'naclsdk_linux_x86.tgz.manifest',
      'glibc': 'toolchain_linux_x86.tar.xz.manifest',
  }
  manifest_file = None
  if sys.platform == 'win32':
    manifest_file = WINDOWS_TOOLCHAIN_MANIFESTS[toolchain]
  elif sys.platform == 'darwin':
    manifest_file = MAC_TOOLCHAIN_MANIFESTS[toolchain]
  elif sys.platform == 'linux2':
    manifest_file = LINUX_TOOLCHAIN_MANIFESTS[toolchain]
  if manifest_file:
    return os.path.join('build_tools', 'toolchain_archives', manifest_file)
  else:
    return None


def ConvertToOSPaths(path_list):
  '''Convert '/' path separators to OS-specific path separators.

  For each file in |path_list|, replace each occurence of the '/' path
  separator to the OS-specific separator as defined by os.path.

  Args:
    path_list: A list of file paths that use '/' as the path sparator.

  Returns:
    A new list where each element represents the same file paths as in
    |path_list|, but using the os-specific path separator.
  '''
  return [os.path.join(*path.split('/')) for path in path_list]


def GetDirectoriesFromPathList(path_list):
  '''Return a list of all the content directories.

  The paths in the returned list are formatted to be OS-specific, and are
  ready to be used in file IO operations.

  Args:
    path_list: A list of paths that use '/' as the path separator.

  Returns:
    A list of paths to be included in the SDK installer.  The paths all have
        OS-specific separators.
  '''
  return ConvertToOSPaths(
      [dir for dir in path_list if dir.endswith('/')])


def GetFilesFromPathList(path_list):
  '''Return a list of all the content files.

  The paths in the returned list are formatted to be OS-specific, and are
  ready to be used in file IO operations.

  Args:
    path_list: A list of paths that use '/' as the path separator.

  Returns:
    A list of paths to be included in the SDK installer.  The paths all have
        OS-specific separators.
  '''
  return ConvertToOSPaths(
      [dir for dir in path_list if not dir.endswith('/')])

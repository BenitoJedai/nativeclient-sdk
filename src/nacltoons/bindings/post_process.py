#!/usr/bin/env python
# Copyright (c) 2013 The Chromium Authors. All rights reserved.
# Use of this source code is governed by a BSD-style license that can be
# found in the LICENSE file.
"""Script for post processing C++ bindings files that are generated by tolua++.

This script performs two replacements.  Firstly it injects our copyright
header.  Secondly it fixes up the toluafix_pushusertype_ccobject calls
in the same way that it is done in cocos.
"""
import sys


def main(args):
  if len(args) != 1:
    sys.exit("Please specify exactly one filename to process")

  filename = args[0]
  with open(filename) as input_file:
    file_data = input_file.read()

    file_data = file_data.replace(r'''#ifndef __cplusplus
#include "stdlib.h"
#endif
''', r'''// Copyright (c) 2013 The Chromium Authors. All rights reserved.
// Use of this source code is governed by a BSD-style license that can be
// found in the LICENSE file.
#ifndef __cplusplus
#include "stdlib.h"
#endif
''')

    file_data = file_data.replace(
        r'toluafix_pushusertype_ccobject(tolua_S,(void*)tolua_ret',
        r'''int nID = (tolua_ret) ? (int)tolua_ret->m_uID : -1;
    int* pLuaID = (tolua_ret) ? &tolua_ret->m_nLuaID : NULL;
    toluafix_pushusertype_ccobject(tolua_S, nID, pLuaID, (void*)tolua_ret''')

    file_data = file_data.replace('*((LUA_FUNCTION*) ', '(')

  with open(filename, 'w') as output_file:
    output_file.write(file_data)

  return 0


if __name__ == '__main__':
  sys.exit(main(sys.argv[1:]))

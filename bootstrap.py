from winreg import *

def getMSBuildPath():
    compatibleVersions = ["14.0", "4.0", "3.5"]
    localMachineRegistry = ConnectRegistry(None,HKEY_LOCAL_MACHINE)
    print("Attempting to read registry keys for MSBuild version '{}':".format("', '".join(compatibleVersions)))
    for version in compatibleVersions:
        try:
            registryPath = r"SOFTWARE\Microsoft\MSBuild\ToolsVersions\\" +version;
            print("Attempting to read registry key '{}':".format(registryPath))
            registryObject = OpenKey(localMachineRegistry, registryPath, 0, KEY_WOW64_64KEY + KEY_READ)
            path = QueryValueEx(registryObject, "MSBuildToolsPath")
            if path != "":
                print("Found path for MSBuild {}: {}\r\nStopping further queries".format(version, path[0]))
                return path[0] + "MSBuild.exe"
            else:
                print("Couldn't read registry key '{}': Return value was empty!".format(registryPath,))
        except EnvironmentError as e:
            print("Couldn't read registry key '{}': {}".format(registryPath, e))

from subprocess import call

import os

def build():
    msbuild = getMSBuildPath()
    project_output_dir = r'2_build'

    if not os.path.exists(msbuild):
        raise Exception('MSBuild not found: {} Make sure at least MSBuild 3.5 is installed!'.format(msbuild))

    project = r"0_vs\thearcadearcade.sln"
    win32_target = '/t:thearcadearcade:rebuild'
    win32 = '/p:Platform=Any CPU'
    print("Building '{}' for Any CPU".format(project))

    # making command line to run
    default = [msbuild]
    default.append(project)    # append a project/solution name to build command-line
    default.append(win32)
    default.append(win32_target)
    default.append('/m:1')  # https://msdn.microsoft.com/en-us/library/ms164311.aspx

    print("Calling '{}'".format(' '.join(default)))
    call(default)

import glob
import shutil
def killBlacklistFiles():
    postBuildBlacklist = [
        "2_build/EMPTY",
        "2_build/*.vshost.*",
        "2_build/*.config",
        "2_build/*.pdb",
        "2_build/*.xml",
        "2_build/dump.txt",
        "2_build/platforms/**/*.nes",
        "2_build/platforms/NES/executable/nestopia.cfg",
        "2_build/platforms/NES/executable/nestopia.log",
        "2_build/platforms/NES/executable/ips/",
        "2_build/platforms/NES/executable/samples/",
        "2_build/platforms/NES/executable/saves/",
        "2_build/platforms/NES/executable/save/",
        "2_build/platforms/NES/executable/screenshots/",
        "2_build/platforms/NES/executable/scripts/",
        "2_build/platforms/NES/executable/states/"
    ]

    for blacklistItem in postBuildBlacklist:
        for filename in glob.iglob(blacklistItem, recursive=True):
            isFile = os.path.isfile(filename)
            print("Removing {}: '{}'".format("File" if isFile else "Folder", filename))
            if isFile:
                os.unlink(filename)
            else:
                shutil.rmtree(filename)

import re
import sys

def set_version(infocs, target_version):
    if not infocs or not target_version:
        raise Exception('invalid param')
        return

    with open(infocs, "r+") as f:
        assemblyinfo_cs = f.read()

        pattern_1 = re.compile(r'AssemblyVersion\(".*"\)', re.MULTILINE)
        pattern_2 = re.compile(r'AssemblyFileVersion\(".*"\)', re.MULTILINE)

        sub1 = r'AssemblyVersion("{}")'.format(target_version)
        sub2 = r'AssemblyFileVersion("{}")'.format(target_version)

        phase_1 = re.sub(pattern_1, sub1, assemblyinfo_cs)
        phase_2 = re.sub(pattern_2, sub2, phase_1)

        f.seek(0)
        f.write(phase_2)
        f.truncate()

set_version(r"0_vs\thearcadearcade\Properties\AssemblyInfo.cs", "0.2.0")
build()
killBlacklistFiles()

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
                return path[0]
            else:
                print("Couldn't read registry key '{}': Return value was empty".format(registryPath))
                return False
        except EnvironmentError as e:
            print("Couldn't read registry key '{}': {}".format(registryPath, e))
            return False

    return True

from subprocess import call

import os

def which(program):
    import os
    def is_exe(fpath):
        return os.path.isfile(fpath) and os.access(fpath, os.X_OK)

    fpath, fname = os.path.split(program)
    if fpath:
        if is_exe(program):
            return program
    else:
        for path in os.environ["PATH"].split(os.pathsep):
            exe_file = os.path.join(path, program)
            if is_exe(exe_file):
                return exe_file

    return None

def restoreNugetPackages():
    nuget = "nuget.exe"
    resolvedNuget = which(nuget);

    if resolvedNuget == None:
        print ('nuget.exe not found. You must manually download nuget.exe and place it inside a PATH-listed directory.\r\n'+
                'Alternatively restore the NuGet-packages of this solution inside Visual Studio once.'.format(nuget))
        return False

    project = r"0_vs\thearcadearcade.sln"
    print("Resolving NuGet-packages for solution '{}'".format(project))

    # making command line to run
    default = [nuget]
    default.append("restore")
    default.append(project)

    print("Calling '{}'".format(' '.join(default)))
    call(default)

    return True

def build():
    msbuild = getMSBuildPath() + "MSBuild.exe"
    project_output_dir = r'2_build'

    if not os.path.exists(msbuild):
        print ("MSBuild not found at '{}'. Make sure at least MSBuild 4.0 is installed.".format(msbuild))
        return False

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
    return True

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
        "2_build/platforms/NES/executable/"
    ]

    for blacklistItem in postBuildBlacklist:
        for filename in glob.iglob(blacklistItem, recursive=True):
            isFile = os.path.isfile(filename)
            print("Removing {}: '{}'".format("File" if isFile else "Folder", filename))
            if isFile:
                os.unlink(filename)
            else:
                shutil.rmtree(filename)

    return True

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

    return True;

if (not set_version(r"0_vs\thearcadearcade\Properties\AssemblyInfo.cs", "0.2.0")):
    print ("Unable to set version inside AssemblyInfo.cs")
    sys.exit()

if (not restoreNugetPackages()):
    print ("Unable to restore nuget packages, but continuing anyway as they might already be in place; if there are errors regarding missing packages, fix this!")

if (not build()):
    sys.exit()
    print ("Building solution failed")

if (not killBlacklistFiles()):
    sys.exit()
    print ("Removing blacklisted files failed. Do not submit this binary, as it might contain copyrighted-material or be broken otherwise.")

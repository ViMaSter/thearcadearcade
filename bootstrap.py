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
    win32_target = '/t:thearcadearcade:Clean,Build'
    win32 = '/p:Platform=x86'
    print("Building '{}' for x86".format(project))

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
        "2_build/platforms/NES/executable/",
        "2_build/GPUCache/"
    ]

    for blacklistItem in postBuildBlacklist:
        for filename in glob.iglob(blacklistItem, recursive=True):
            isFile = os.path.isfile(filename)
            print("Removing {}: '{}'".format("File" if isFile else "Folder", filename))
            try:
                if os.path.isfile(filename):
                    os.unlink(filename)
                elif os.path.isdir(filename):
                    shutil.rmtree(filename)
            except Exception as e:
                print("Error deleting file {}:".format(filename))
                print(e)
                return False

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

def clearBuildDirectory():
    folder = '0_vs/thearcadearcade/bin/Release'
    if os.path.isdir(folder):
        print ("Attempting to clear build intermediate folder '{}'".format(folder))
        for the_file in os.listdir(folder):
            file_path = os.path.join(folder, the_file)
            try:
                if os.path.isfile(file_path):
                    os.unlink(file_path)
                elif os.path.isdir(file_path):
                    shutil.rmtree(file_path)
            except Exception as e:
                print(e)
                return False
    else:
        print ("Build intermediate folder '{}' doesn't exist; skipping it".format(folder))
           
    folder = '2_build'
    if os.path.isdir(folder):
        print ("Attempting to clear build intermediate folder '{}'".format(folder))
        for the_file in os.listdir(folder):
            file_path = os.path.join(folder, the_file)
            try:
                if os.path.isfile(file_path):
                    os.unlink(file_path)
                elif os.path.isdir(file_path):
                    shutil.rmtree(file_path)
            except Exception as e:
                print(e)
                return False
    else:
        print ("Build intermediate folder '{}' doesn't exist; skipping it".format(folder))
    return True

def createPlaceholderFiles():
    executableMapping = {
        "NES": "nestopia.exe"
    }
     
    for platform in executableMapping:
        try:
            filename = "2_build/platforms/{}/executable/PLACE_{}_HERE".format(platform, executableMapping[platform])
            os.makedirs(os.path.dirname(filename), exist_ok=True)
            with open(filename, "w") as placeholderFile:
                placeholderFile.write("Stop reading this and place an emulator here. : >")
                placeholderFile.close()
        except IOError as e:
            print("Error writing the placeholder-file for platform '{}' (executable file '{}'); exception info: {}".format(platform, executableMapping[platform], str(e)))
            return False
            
        return True
    
if (not set_version(r"0_vs\thearcadearcade\Properties\AssemblyInfo.cs", "0.2.0")):
    print ("Unable to set version inside AssemblyInfo.cs")
    sys.exit()

if (not restoreNugetPackages()):
    print ("Unable to restore nuget packages, but continuing anyway as they might already be in place; if there are errors regarding missing packages, fix this!")

if (not clearBuildDirectory()):
    print ("Couldn't clear build directory")
    sys.exit()


if (not build()):
    print ("Building solution failed")
    sys.exit()

if (not killBlacklistFiles()):
    print ("Removing blacklisted files failed. Do not submit this binary, as it might contain copyrighted-material or be broken otherwise.")
    sys.exit()

if (not createPlaceholderFiles()):
    print ("Couldn't create placeholder files; the build is still valid, but no placeholder have been created")
    sys.exit()

# The Arcade Arcade

Collecting highscores in old games? Why not collect scores for collecting scores in old games?

## Requirements
1. Download [any release](../../releases/) of this project
2. Extract the contents into an empty folder
3. Set up all potential emulators (see [Setting up emulators](#setting-up-emulators))
4. Run `arcadearcade/arcadearcade.exe`

## Setting up emulators
1. Navigate to where you've extracted the project
2. Navigate to `/platforms/[PLATFORM]`
3. Reference to the contents of `config.json` for compatiable emulators
4. Create an `executable`-folder inside `/platforms/[PLATFORM]/`
5. Extract any emulator into the `executable`-folder. (The emulator's executable should now live at `/platforms/[PLATFORM]/executable/{executable.name}.exe`

## Contributing
Pull requests are accepted; simply clone this repository and set the working directory of the project to `../../../../1_dependencies` to start hacking away. Visual Studio will complain about relative paths but accept them nevertheless.

## Building
### Automated build process
Requires **Microsoft Build Tools 2015** and **python 3.4+**:  
Simply run `python bootstrap.py build`. The compiled binary will live as `thearcardearcade.exe` inside `/2_build`.

### Manual build process
Requires **Microsoft Visual Studio 2015/2017**:  
Open `/0_vs/thearcadearcade.sln` and build+run either a "Debug" or "Release"-build.

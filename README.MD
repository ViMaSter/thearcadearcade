# The Arcade Arcade

Collecting highscores in old games? Why not collect scores for collecting scores in old games?

## Playing
1. Download [any release](../../releases/) of this project
2. Extract the contents into an empty folder
3. Follow the extracted README.MD inside the .zip for further instructions

## Contributing
Pull requests are accepted; simply clone this repository and set the working directory of the project to `../../../../1_dependencies` to start hacking away. Visual Studio will complain about relative paths but accept them nevertheless.  
All branches except `master` could be rebased at any time; if you rely on work of them, make sure you're talking to the main owner of that branch or fork it using GitHub and submit a pull request.

### Building
#### Automated build process
Requires **Microsoft Build Tools 2015** and **python 3.4+**:  
Simply run `python bootstrap.py build`. The compiled binary will live as `thearcardearcade.exe` inside `/2_build`.
Afterwards, run `python run.py` to start the build. This will run the build produced, but using the ROMs + emulator-executables inside `1_dependencies`, as they are stripped from the build's target directory.

#### Manual build process
Requires **Microsoft Visual Studio 2015/2017**:  
Open `/0_vs/thearcadearcade.sln` and build+run either a "Debug" or "Release"-build.

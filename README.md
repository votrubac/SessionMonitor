# User Session Monitor
This is a Windows service that kills specified processes for a user when their Windows session is dissconnected.

If a user locks their Windows session, it will kills processes after the configured time interval (unless the user unlocks the session before that).

## Installation Instructions
1. Copy the “SessionMonitor.zip” installation archive to a local folder
2. Right-click the installation archive and select “Properties”
3. Check “Unblock” and click “OK”
4. Extract the installation archive
5. Open the command prompt as administrator and navigate to the folder with extracted files
6. Type “Install”

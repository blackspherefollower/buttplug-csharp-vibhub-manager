# VibHub Manager implementation for Buttplug

**Note: This is an unsupported PoC quality library. Do not use this as an example of how to build device sub-managers for Buttplug**

## Support

If you use this manager, things might go wrong, possibly affecting other aspects of the Buttplug server.
Until completely removed (deleted and the Buttplug server is reinstalled from scratch), do not expect help from the community.

## Usage

1. Build the library
2. Drop the built `Buttplug.Server.Managers.VibHubManager.dll` and `SocketIOClient.dll` into the same directly as the Buttplug server binary and other manager DLLs (this is a **very very unsupported** way of extending Buttplug)
3. Create a file named `vibhub.txt` in that same folder containing the device IDs of each your your VibHubs, one per line
4. Start the Buttplug server with INFO logging and you should see the VibHub manager get loaded and get invoked during scanning
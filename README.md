SkyDrive.FileWatcher
====================
Package is made for tracking files transformations in SkyDrive.

To use this package start with registering your app in Live Connect Developer Center and get Client ID.

The controller to create a connection with Live API is integrated in this package. It is responsible for authentication, reading and recording data. It is asynchronous yet it can not work in sync while authentication. In fact, it is not possible to initialize couple controllers at a time, it will lead to exception. At the same time FileWatcher cannot work in one stream, all the more so in one stream with the app. Therefore if apart from tracking the file transformation you also need to read something from SkyDrive (which is quite logical) you'll have to initialize the controller once and use it, also transmit it to FileWather's constructor.  
You can also create a controller of your own, implement ILiveController interface (can be found in the package) and use it for both your needs and for the FileWatcher, but don't disregard synchronization.

Getting started
---------------
```
var watcher = new FileWatcher("CLIENT_ID", "demo.txt");
watcher.Start();
watcher.Changed += (sender, eventArgs) =>
{
	var newValue = eventArgs.Value;
	...
};
```

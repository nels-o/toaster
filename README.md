toaster
=======

A wee Win8 console notifications app. Post toast notifications from the console, making it easy to integrate into 
existing batch scripts etc.

##Deps

Note that if you want to build this application from source you will need the [Windows API CodePack](http://archive.msdn.microsoft.com/WindowsAPICodePack/Project/License.aspx). This is required
because the application has to ensure that it is registered with the Windows Start screen in order to post toast. 
Registration basically entails adding a shortcut for the app to the start screen.

##Features

At present there is support for 

* Images
* Title text
* Message text

Eventually I'll get the sound portion working as well, and in the long term, support for all of the templated toast 
types. 

# License

For now I'm releasing this under the Creative Commons Attribution license http://creativecommons.org/licenses/by/3.0/us/
By for now, I mean, basically forever, pending a release to the public domain. Until such time as that, please include
a link to (this page)[https://github.com/nels-o/toaster] with any use you make of the provided source.

resx-localize
=============

Easily maintain your .resx file translations.

Two folders, one contains the source code, the other is a working example of how it works.

=============

*A few points about the example:*

* See .config to put in your keys and other settings. I've commented some settings there.

* Then place as many .resx files as you need, into the same folder as the EXE. The example has one.

* Create a .job.xml file for each .resx, with the same name. This is a simple xml file that defines the source and target languages and the comments to send to the translator.

* The application is command line driven. Just run the EXE to perform the translation.

* Translation will be sent up in groups of pre-configured size, so you may need to run more than once if it's a big job.

* Run the EXE again to check for any outstanding translation completions, considering a query throttle. You can do this as many times as needed until all the translations come back as complete.

* The EXE accepts the following parameters: "/get" -> Get only, "/send" -> Send only, "/getnow" -> get now, bypassing query throttle. Running without parameters does a send, if any needed and a get.

* The application keeps track of the state of each source translation individually, and you can change / add / remove text from the source resource at any time and it will re-translate only those portions allowing you to evolve your application without re-translating the entire .resx.

* I've also created a small batch file to convert the resulting text based .resx files into compiled .DLL files that you can place right into your application's deployment. This is really outside of the scope of this app, but makes translation deployment a snap.


* Basically, you place a bunch of .resx files in a folder, then you set up a simple .xml config file specifying your API keys, destination languages and other parameters. Then you run an EXE and it automatically handles everything. It groups your requests, sends them out to MyGengo, and checks for completion using a throttle time. Then, it places the translated text into destination .resx files. One per language.

* It even handles special cases with prefixes and suffixed. E.g. "_& Some text" will be translated and "Some text" and the prefix "_& " will be pre-pended when the translation comes back. This is so as not to confuse the translators with programming specific formatting. It also supports adding comments to the translator, etc...
# Seq.Client.WindowsLogins

This is a fork of the [Seq.Client.EventLog](https://github.com/c0shea/Seq.Client.EventLog) app for [Seq](https://getseq.net/).

This substantially modifies the client to a service that looks for successful interactive user logins and raises a nicely formatted event with the data extracted as structured properties.


## Get Started

1. [Download the latest release](https://github.com/MattMofDoom/Seq.Client.WindowsLogins/releases) of Seq.Client.WindowsLogins.
2. Extract it to your preferred install directory.
3. Edit the ```Seq.Client.EventLog.exe.config``` file, replacing the ```SeqUri``` with the URL of your Seq server. If you configured Seq to use API keys, also specify your key in the config file.
4. From the command line, run ```Seq.Client.WindowsLogins.exe /install```. This will install the Windows Service and set it to start automatically at boot.
5. From the command line, run ```net start Seq.Client.WindowsLogins``` to start the service.
6. Click the refresh button in Seq as you wait anxiously for the events to start flooding in!

## Enriched Events

Events are ingested into Seq with useful properties that allow for easy searching.

```
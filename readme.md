# MIDItoBMS

## Summary

*MIDItoBMS* is a command-line program I've been developing purely for fun and to put my reverse-engineering knowledge to the test. Simply put, it allows for the conversion of MIDI files to BMS files for SMS.

The *MIDItoBMS* converter also supports loop-point controllers from the EMIDI specifications in order to specify loops. Various modes and options can also be enabled or configured through optional arguments in the command line. Even more BMS features not directly supported by *MIDItoBMS* can still be used indirectly via sequencer-specific events.

Specifically, the goal of this program is to output a BMS file with equivalent contents to that of a given MIDI file, losslessly. This definition means it is unreasonable to expect a MIDI file originally converted from a BMS file to be put through the program and result in a BMS file identical to the original — while MIDI > BMS is a lossless conversion, BMS > MIDI is not.

## Usage

*MIDItoBMS* has two command-line modes; which is used depends on how many arguments are specified.

If only a single argument is passed, *MIDItoBMS* will interpret the argument as an absolute or relative path to the MIDI file to convert, and convert said MIDI with the all of the default settings. This enables quick-and-easy drag-and-drop usage.

If two or more arguments are specified, the entire command line will be interpreted as list of options. Each option begins with a hyphen ("-") and the name of the option (case insensitive). Options can have zero or more arguments.

The options must be one or more of the following:

|Option|Description|
|------|-----------|
|_**-input** \<file\>_ <br> _**-output** \<file\>_|Specifies the relative, or absolute, input- and output- file paths. Only ***-input*** is required; by default, the output file will be the input file with the ".bms" extension appended.|
|***-ignoremidiprograms*** <br> ***-ignoremidibanks*** <br> ***-ignoremidipitchbends*** <br> ***-ignoremidipans*** <br> ***-ignoremidivolumes***|Disables the support for MIDI events or controllers of the specified type. They will be ignored when encountered. Instead, sequencer-specific events must be used to change the respective variables. By default, these MIDI events and controllers are not ignored.|
|_**-trackdetection** \<mode\>_|Sets which mode is used to detect root and child tracks in the MIDI. By default, automatic track detection is enabled.|
|***-addtrackinit***|Enables the automatic addition of the track-initialization callback command (E7) to each detected child track (not the global/root track). The argument passed to each callback will default to zero (unless overwritten by the track's meta data). By default, E7 commands are not added to the beginning of each child track.|
|_**-velocityscale** \<file\>_|Changes the scale of the velocity of all the note-on events in the input MIDI. *\<scalar\>* is a double-precision floating-point number. Values less than one soften notes, while values greater than one amplify notes. The scaled velocities will still be clamped to the range 0 - 127. By default, the velocity scalar is one.|
|***-skippitchrange***|Skips the automatic addition of a pitch-range command being added to child tracks. Useful if the MIDI ends up overriding it anyway. By default, the pitch range is set to +/- 2 semitones (the default as per the MIDI standard).|
|_**-perfduration** \<pulses\>_|Enables performance-control interpolation by putting the constant value *\<pulses\>* (specified as a 16-bit, unsigned integer) as the duration of each performance-control command. May result in the a smoother-sounding BMS. Low values are recommended. Default value is zero (disabled).|
|***-batch***|Disables any usage of stdin. This makes it useful for batch usage, as the program will run and end without pausing for input. By default, this is disabled so drag-and-drop users are able to read and analyze any ouput (including error messages) in the console window.|

## Track Detection

BMS files are played using a single, root track, which later creates a hierarchy of child tracks asynchronously playing separate sections of the BMS. In order to emulate this in *MIDItoBMS*, there must be one track designated as the root track used to create the child tracks. Currently, only a single generation of child tracks is supported — this is because MIDI files do not have track hierarchy. Thus, a limit of 16 tracks per BMS (excluding the root track) is imposed.

In any track detection mode, if no suitable root track is found, the MIDI file is in error and the conversion fails. Child tracks are optional and a suitable BMS file will still be created if there are no child tracks detected.

There are various modes of track detection in *MIDItoBMS*, described below.

### Automatic

This mode is enabled by default or by setting the ***-trackdetection*** option to *auto*.

The root BMS track and its the children tracks can be detected automatically by analyzing the types of events on each MIDI track. The root track will be the first MIDI track (in chunk order) which contains no channel events (excluding control-change events) ? thus, only meta, SysEx, and control-change events are expected.

Child tracks are detected as all tracks containing at least one note-on event. The index into the children array (i.e. track ID) is determined by the channel number of the first channel event on the track (not necessarily a note on). All subsequent channel events in the track whose channel number does not match the track ID will be ignored and not converted to the output BMS.

### Manual

This mode is enabled by setting the ***-trackdetection*** option to *manual* and is useful in the event that the default, automatic track-detection mode does not produce suitable output.

In this mode, tracks are manually mapped to BMS tracks using the track's meta data.

## Meta Data

*MIDItoBMS* supports assigning meta data to tracks in order to specify various properties about a certian track. It does this using the value of the first track-name  MIDI event on the track. All meta data is case insensitive.

The meta data is formatted in a comma-separated list of key-value pairs. The keys and values are separated by colons. Key names and values may consist of alphanumeric characters, hyphens, and full stops. The following table describes the defined keys and their values:

|Key|Description|
|---|-----------|
|***track-type***|Specifies the type of track. Possible values are *root* and *child*. If *child* is specified, then the ***track-id*** key specifies the track ID for the child track.|
|***track-id***|Specifies the track ID of the track. Must be a whole number between 0 and 15 (inclusive).|
|***track-arg***|Specifies the argument to pass to the track-initialization command on the track, as a 16-bit, signed integer. Only used if the ***-addtrackinit*** option is specified on the command line. If this key is not present, a default value of zero is assumed.|

Note that for all integer keys, the value is assumed to be specified in decimal. To specify the value in hexadecimal, suffix the value with the letter "H" (case insensitive).

## Implementation

*MIDItoBMS* contains a basic implementation of the features defined in the MIDI standard.

- Split SysEx messages are **not** supported.
- Data bytes are allowed within SysEx and sequencer-specific events.

### Time Divisions

The MIDI header contains the time division. The MIDI standard defines the PPQN and SMPTE time divisions. *MIDItoBMS* supports only the PPQN time division. A value of 120 PPQN is recommended.

### Channel Events

*MIDItoBMS* implements most channel events. Only channel events whose channel number matches that of the current track's track ID will be converted. The following table notes how each channel event is converted into a BMS command (note that BMS commands are specified in hexadecimal):

|MIDI|BMS|Notes|
|----|---|-----|
|Note On|00-7F|Note ons with a velocity of zero will be converted to BMS voice-off commands (see below). Voice IDs are assigned automatically. Only 7 simultaneous voices are allowed per track (the voice ID 0 is **not** supported by *MIDItoBMS*).|
|Note Off|81-87|89-8F are **not** supported. Velocity is ignored.|
|Program Change|A4|Only programs 0-127 are selectable. To select other programs, you'll need use sequencer-specific events.|
|Pitch Bend|9C-9F|16-bit direct form is always used. Other forms are **not** supported. Durations are supported via the command-line.|
|Note Aftertouch| |**Not supported.**|
|Channel Aftertouch| |**Not supported.**|
|Controller|???|Some controllers are supported (see below).|

### Meta Events

The following meta events are supported by *MIDItoBMS*:

|MIDI|BMS|Notes|
|----|---|-----|
|Tempo Change|FD|Default tempo is 120 BPM (for the root track) or inherited from the parent (for child tracks). Should be placed on the root track; otherwise, you might get unpredictable results.|
|Track Name| |Used only to store meta data for the track.|
|Sequencer-specific| |Used for inserting raw BMS commands.|
|End of Track|FF|Ends the track. (Note that when a track ends, all of descendant tracks are automatically ended as well).|

### Controllers

Only a handful of controllers are implemented in *MIDItoBMS*. The combined value of the MSB and LSB for the 14-bit continuous controllers are calculated as stated in the MIDI standard (para. 2, p. 12). The following table shows the supported controllers (controller numbers are displayed in hexadecimal):

|LSB|MSB|Name|BMS|Notes|
|---|---|----|---|-----|
|0|20|Bank Select|A4/AC| |
|7|27|Volume|9C-9F| |
|A|2A|Pan|9C-9F| |

## Inserting Raw BMS Commands

To avoid various shortcomings of the MIDI standard, *MIDItoBMS* supports sequencer-specific events which allow you to directly place BMS commands into the command stream. Using this, you can have full-precision performance controllers, select all programs in an IBNK, etc.

In order to get *MIDItoBMS* to read a sequencer-specific event, the event have a MIDI ID of 125 (7D in hexadecimal). If the event does not have a MIDI ID of 125, the event is ignored. The bytes following the MIDI ID is the raw data for the BMS command in its entirety; this data is placed as-is into the BMS.

- BMS files are in *big-endian* byte order (i.e. most-significant byte first); as such, the BMS command data is expected to be provided accordingly. *MIDItoBMS* does **not** perform endian swapping automatically.
- While it is possible to insert BMS commands utilizing raw file offsets (such as the C4 and EE commands), it is not recommended to use such commands as the required offsets might be unknown.

Thus, an example to select the 128ᵗʰ program in an IBNK would be the event ```FF 7F 04 7D A4 21 80```.

## Looping

Loops specify that a segment of a sequence must repeat a specified number of times, or indefinitely. They may appear in any track, be it root or child. *MIDItoBMS* supports most of the BMS looping functionality by implementing the loop controllers in the EMIDI standard (**CC \#116-119**).

### Local Loops

**CC \#116** sets the begin point of the loop, while **CC \#117** sets the end loop point. All events after a **CC \#116** and before its **CC \#117** will be included in the loop, even if they occur on the same pulse as the loop point. Note that an indefinite loop 

### Global Loops

**CC \#118** and **CC \#119** are global loop start and end points, respectively. They work exactly to their local counterparts, but define loop points for all tracks ? not just the one on which they are placed. You can use these to simplify syncing loop points among all tracks in a MIDI file.

### Nesting Loops

Loops may be nested so long as each has its corresponding loop end point controller. Loops are kept track of using a stack, so a loop end point corresponds to the last encountered loop begin point that hasn't already been matched with a loop end point. In other words, loops open and close just like nested XML tags, braces, parentheses, etc.

### Fixed vs. Indefinite Loops

The value associated with the loop begin controller indicates how many times to loop. A value of zero indicates an indefinite loop, while any greater value indicates the number of times to loop (a fixed loop). A loop is described here as how many times the segment of the sequence is to be repeated ? not how many times it is to be played (i.e. a loop count of 3 will make the segment play a total of 4 times). The value of a loop end controller (both local and global) must be 127, as per the EMIDI specifications.
# Where the editor came from

The chord editor was not designed on paper. It was asked for in a few messages by the
maintainer to an AI model (Claude, in Claude Code), built, tried by him, and changed.
These are his messages, word for word, slips of the keyboard and all, with what was
built from each. They are here because they say what the editor is *for* better than
any page of rules: when a rule in `spec/` leaves you unsure, this is what it was
trying to achieve.

They were written about Simple Presenter, the Linux app the editor was built in, and
speak of things in it that are not part of this app (its stage display, its Libraries
header).

## The brief, 9 October 2026

> Hey let's add a chord editing mode.  ProPResenter supports a  linked text option (used typically on stage layouts) for Chords (Multitracks) with options for chord/numbers/numerals/do re mi notation - this is an additional option with textboxes like to current slide or next slide text.  The chords can be optionally added to the data files (see the proto) - I cant remember exact details - but they are linked to the text.  So a chord editor would have to offer some UI to insert chords, using the text and anchors.. (OR maybe show the text as chord pro format and let user type chords as chord pro).  I like to offer both styles of editor...Chordpro text display/edit and a nice GUI display/editor.  now this one will be tricky to decide how best to do it...I have a gut feeling there is a real opportunity for a nice new/novel editing method...We have to be mindful that text lives in side seperate text elements on seperate slides - so I'm not sure about any editor experience that shows the entire text as a single slab of text - But that would be a nice way to view/edit chordpro text...and perhaps the gui should also show the song as a continuous slab of text - my guy says one slide at a time woudl feel klunky...
> So editor might have three modes - normal mode (just like propresenter) and GUI chord editor (pro does not have this) and chordpro chord editor (pro does not have this).  PRo has no editor - it just imports songs with chords frmo multitracks.  There is a multitracks flag in the file - let's leave that off! hand edited files are not form them.  Btw the file also stores oringial key - and the UI offers to change the key used when chords are displayed (a live change - not changing the file). I kinda want to explore ideas for gui chrod editing... I think the chrodpro option is mostly obvious - a mode where the song text appears as a continous slab of text that hte user can edit and enter chord pro chords - but it would be nice if that chord pro text editor had text editing limitations - for a first version let's only let them insert/edit/delete chord pro chord text - the song text is read-only in this mode - this avoid reconilling changes acrros slides and various text formatting.  Now for the GUI editor....I dont know what I want...so let's put out some thooughts...  a slick, smooth fast UI...intuitive and nice and joy to use...picture moving your mouse acrros the text and the chord insertion point preview is live appearing - following your mouse as you move around - learly giving live feedback of the chord insertion point if you click....clicking an "empty/no chord" insertion point than inserts a chord - graphical element like a bubbleround rect with a vertical tail,arrow pointing down to clearly show the insertion point (and the top part of the raphci chord element is where you see /edit the chord...)  now we could type the chord- and that should work - but limit the text entry to sensebile chordpro text - eg first char can only be a root note letter - right? and then instead of fully typing the chord quality indicator maybe the first letter offers a list of clickable (or arrow tab autocompletable ) options... same with the following extension/alteration and or the / bass note....  I havent invented yet.. but simply typing this with smart text enforcing/filtering is "not enough"  - I want affordances that help with speed of input!  Let's see what you can invent and we can follow an agile approach - I'll use it and maybe get ideas from using. being able to drag chords around in the gui mode is good. maybe copy/paste.  (maybe selecting the KEY helps with smart filtering the autocompelte/speed choices offered?) DEfault key can be C. The chord editor modes are editor sub modes - let's add editor toolbar buttons to toggle them.  (oh, and I guess we add a song import option for chordpro - that does not make sense in the editor - because editor is existing file..hmm where to expose importing a new song/presentation from chordpro file (and later more import options...I think we can add a + button to the right side of the LIBRARY header and give a popup menu -with import ChordPro file.  Btw, you should add an option to that popup menu to add a new library folder!  Importing a chordpro textfile should follow any sensible directives (creating groups like verse, chorus if they are there and default to splitting text into slides, two lines per slide (with option to change that.) It should import the the selected library folder..

A minute later:

> hold on.. I added a folder with a few example files with chords (some/most) came from multitracks - some might be created/edited by me. ~/Documents/Chords/ ...if that is enough info - go ahead!

**What was built from it.** Three ways of working on a song, switched between by
buttons: the slides as before, the chord sheet, and ChordPro text. Both chord editors
show the whole song at once, though the file has it slide by slide. On the sheet: the
insertion point that follows the pointer, the bubble with its tail, typing held to
what could be a chord, a list of what the chord might be going to be, the key's own
chords on the keys 1 to 7 (the answer to "affordances that help with speed of
input"), dragging, and copying a line's chords word for word onto another. In
ChordPro: only the chords can be changed. The key defaults to C. Multitracks' flag is
never set. How ProPresenter keeps chords "linked to the text" was worked out from the
example songs he then pointed to: [01-file-format.md](01-file-format.md).

## After trying it, the same day

> This is great.  some thoughts/feedback.  First - nice job on the GUI chord editing.. On the stage display-> The chords are displayed a little to high above the text box that is configured to show them - can you lower them by about 50%. Also, in the gui chord editor - the assumption that inserting chords is only as start of words - I think is wrong.  Chords can be played part-way through a word (surely one of the example chord files did this).. but at any rate - let's allow any insertion point that the file format supports (so any character)....And I think we could make the different slides clearer in the edit - probably a horizontal dividing line. (or maybe a rounded rect around the slides text - yeah let's try that - nto all different sizes - better size them to fit widest/tallest text in the presentation).  I also think there is a bug in the live transposing - it might only be working/displaying for the current slide -I have a stage layout with chords currently configured - so can you run some checks on all the chord transpose/linked text display logic

**What changed.** The spot goes to any character, not only the starts of words
([03-chord-sheet.md](03-chord-sheet.md), rule 16), with Shift to keep to words. Each
slide's lines are in a rounded card, all cards one size (rules 7 and 8). The other two
points were about Simple Presenter's stage and are not part of this app.

## That evening

> oh I am really enjoying the chord editor... here comes a lot of work to do overnight! (I will review in morning and fix any problems)
> one fine tune.. to enable faaster pro input...  If I am typing a chord, continue to show the insertion indicator if I move my mouse over lyrics text and if I click and new insertion point then commit my current typing chord (as typed).
> Right now, the in progress editing chord is lost.?
> This experienc of editing chords with the gui ditor is nice - let's talk it up int he doco (add a screenshot of it in the chord editor section).    
> might as well add a screenhsot of the chordpro text editor as well..

(The message went on to other things about Simple Presenter, left out here.)

**What changed.** Rules 38 to 40 of the sheet: the spot goes on following the pointer
while a chord is being typed, and a click on another place keeps the chord and starts
the next. Click, type, click, type.

## The plan for this app, 10 October 2026

> Let's add the lic later..Next I plan to start new CLaude code sessions on my mac and on my windows machine -the goal to build a standalone chord editor (that works the same as this wondeful chord editor we made in simple presenter)
> I have some fear that it might take a lot of promptign to get the same result - so my idea is this:
> 1. You know my previous prompts (right? or perhaps can advise me how to extract them and give to to you now) about that feature and you wrote the code - you could provide the best info/prompts to to give to new claude sesssion to quicky replicate the nice dual mode editor. But you could also augment that knowledge with the following:
> 2. I already have a Windows version I hand coded with bugs I have ignored.... https://github.com/greyshirtguy/Pro7ChordEditorWin and for this project on my other machines - I and want to update that. (I'd like to also mac a mac version -so I assume a good path is to rename the repo - dropping the Win part of the name and moving the windows app to a win folder (alongside a mac folder) - unless you thinks its better to have sepearate repo - I am totally open to patform speciic repos)
> 3. There are bugs in the win version  - that might have created incossient files - take a good look at the existing code and the issues logged - you might hopefully be able gleam some awareness of date issues that might have been created in .pro files previously edited (and make the new updated app -aware and able to fix/handles those if encountered)... 
> 4. A standlone app needs a paradigm to show the user their pro library and pro files see my existing win ap pfo rhow I did that - feel free to use that or improve it.
> 5. I think an automated invisible backup system (keeping files in local app data) would be a good new feature - and surface that backups exist for any previously edited pro file (make a nice ui for restoring and undoing a restore) - keep the checks that pro must be closed..
> 6. OBviously we cant build the new mac app and update the windows app from this session on my linux box - so give me a plan/handover/steps to help start new sessions on my mac and windows box (so those local agents can benefit from build/test cycle) - I dunna whats a best apprach here? a handover doc? add logic and UI explaining comments to the code for the chord editor (I especially want to take advangtage or your knowledge of the feature and have you consider and advise best approach to replicate natively on Mac and Win)
> 7. Mac and be a normal SwiftUI xcode project. Win can stay in .net - but happy to move to a modern/best supported UI framework.
> 8. What have I forgetton to think about? Maybe you have advice to approach this in a more effective manner than the one I am suggesting.
> Ideally a high level advice and next steps would be great!

The answer to that is this repository: `spec/`, `testdata/`, `reference/`. What was
found in the old Windows editor's code and issues is in
[08-repair.md](08-repair.md).

## What he then decided

> One repo! native. MIT license. no blocking Multitracks files. Oh I need to renew my apple dev paid account - it just lapsed - I will renew (do I need to do that first?) No AppStore (a bit put off by the rules/limitations) - (Im the sort that would possible use a private API if I felt it ws stable and enabled a huge feature!). Yes write the pack and push to the repo (my assumption is the platforms specific claude session can just clone and use that to be guided.) Okay I will do MAc first!

Those are the decisions in the table of [00-overview.md](00-overview.md).

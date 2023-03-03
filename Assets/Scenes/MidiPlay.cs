using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using UnityEditor.VersionControl;
using UnityEngine;

public class NoteOn
{
    public string NoteName { get; set; }
    public int NoteNumber { get; set; }
    public float Time { get; set; }
    public int Length { get; set; }

    public NoteOn(string n, int nn , float t, int l)
    {
        NoteName = n;
        NoteNumber = nn;
        Time = (float)(t * 0.001);
        Length = l;
    }
}

public class MidiPlay : MonoBehaviour
{

    private const string OutputDeviceName = "Microsoft GS Wavetable Synth";

    private OutputDevice _outputDevice;
    private Playback _playback;
    private List<NoteOn> noteOnList = new List<NoteOn>();
    [SerializeField] ParticleSystem myparticle = null;
    private float x_movement = 0.005f;
    // Start is called before the first frame update
    void Start()
    {
        InitializeOutputDevice();
        var myFile = MidiFile.Read("Assets/MidiFiles/debussy-clair-de-lune.mid");
        //var midiFile = CreateTestFile();
        InitializeFilePlayback(myFile);

        var tempoMap = myFile.GetTempoMap();
        IEnumerable<Note> notes = myFile.GetNotes();

        foreach (Note note in notes)
        {
            var timeInMilliSeconds = ((TimeSpan)note.TimeAs<MetricTimeSpan>(tempoMap)).TotalMilliseconds;
            var lengthInSeconds = ((TimeSpan)note.LengthAs<MetricTimeSpan>(tempoMap)).TotalSeconds;
            //Debug.Log("notenum: " + note.NoteName + "  " + timeInSeconds + "  " + lengthInSeconds);

            NoteOn noteOn = new NoteOn(note.NoteName.ToString(), note.NoteNumber, (float)timeInMilliSeconds, (int)lengthInSeconds);
            noteOnList.Add(noteOn);

            // Debug.Log(note.ToString());
            //Debug.Log("notenum: " + note.NoteNumber +
            //    " notename: " + note.NoteName +
            //    " length: " + note.Length +
            //    " time: " + note.TimeAs<MetricTimeSpan>(tempoMap).TotalMilliseconds +
            //    " velocity: " + note.Velocity);

        }

        StartPlayback();
    }

    // Update is called once per frame
    void Update()
    {
        transform.Translate(x_movement, 0, 0);
        if (Mathf.Abs(transform.position.x) >= 8)
        {
            x_movement = -x_movement;
        }
        NoteOn firstNote = noteOnList.First();
        if (firstNote.Time <= (Time.time))
        {
            BurstIt();
            noteOnList.Remove(firstNote);
        }

    }

    public void BurstIt()
    {
        myparticle.Play();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Releasing playback and device...");

        if (_playback != null)
        {
            _playback.NotesPlaybackStarted -= OnNotesPlaybackStarted;
            _playback.NotesPlaybackFinished -= OnNotesPlaybackFinished;
            _playback.Dispose();
        }

        if (_outputDevice != null)
            _outputDevice.Dispose();

        Debug.Log("Playback and device released.");
    }

    private void InitializeOutputDevice()
    {
        Debug.Log($"Initializing output device [{OutputDeviceName}]...");

        var allOutputDevices = OutputDevice.GetAll();
        if (!allOutputDevices.Any(d => d.Name == OutputDeviceName))
        {
            var allDevicesList = string.Join(Environment.NewLine, allOutputDevices.Select(d => $"  {d.Name}"));
            Debug.Log($"There is no [{OutputDeviceName}] device presented in the system. Here the list of all device:{Environment.NewLine}{allDevicesList}");
            return;
        }

        _outputDevice = OutputDevice.GetByName(OutputDeviceName);
        Debug.Log($"Output device [{OutputDeviceName}] initialized.");
    }

    private MidiFile CreateTestFile()
    {
        Debug.Log("Creating test MIDI file...");

        var patternBuilder = new PatternBuilder()
            .SetNoteLength(MusicalTimeSpan.Eighth)
            .SetVelocity(SevenBitNumber.MaxValue)
            .ProgramChange(GeneralMidiProgram.Harpsichord);

        foreach (var noteNumber in SevenBitNumber.Values)
        {
            patternBuilder.Note(Melanchall.DryWetMidi.MusicTheory.Note.Get(noteNumber));
        }

        var midiFile = patternBuilder.Build().ToFile(TempoMap.Default);
        Debug.Log("Test MIDI file created.");

        return midiFile;
    }

    private void InitializeFilePlayback(MidiFile midiFile)
    {
        Debug.Log("Initializing playback...");

        _playback = midiFile.GetPlayback(_outputDevice);
        _playback.Loop = true;
        _playback.NotesPlaybackStarted += OnNotesPlaybackStarted;
        _playback.NotesPlaybackFinished += OnNotesPlaybackFinished;

        Debug.Log("Playback initialized.");
    }

    private void StartPlayback()
    {
        Debug.Log("Starting playback...");
        _playback.Start();
    }

    private void OnNotesPlaybackFinished(object sender, NotesEventArgs e)
    {
        LogNotes("Notes finished:", e);
    }

    private void OnNotesPlaybackStarted(object sender, NotesEventArgs e)
    {
        LogNotes("Notes started:", e);
    }

    private void LogNotes(string title, NotesEventArgs e)
    {
        // Looks like I actually have to use below format to get string value of note properties
        var message = new StringBuilder()
            .AppendLine(title)
            .AppendLine(string.Join(Environment.NewLine, e.Notes.Select(n => $"  {n}")))
            .ToString();
        //Debug.Log(message.Trim());

    }
}

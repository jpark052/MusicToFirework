using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Melanchall.DryWetMidi.Common;
using Melanchall.DryWetMidi.Composing;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Multimedia;
using Melanchall.DryWetMidi.Standards;
using UnityEngine;
using UnityEngine.VFX;

public class NoteOn
{
    public string NoteName { get; set; }
    public int NoteNumber { get; set; }
    public float Time { get; set; }
    public int Length { get; set; }
    public int Velocity { get; set; }
    public  Color color { get; set; }
    public NoteOn(string n, int nn , float t, int l, int v, Color c)
    {
        NoteName = n;
        NoteNumber = nn;
        Time = (float)(t * 0.001);
        Length = l;
        Velocity = v;
        color = c;
    }
}

public class MidiPlay : MonoBehaviour
{
    [SerializeField] private VisualEffect visualEffect;
    //private static readonly int ColorPropertyID = Shader.PropertyToID("_Color");
    //public SpawnEvent parentSpawnEvent;
    public ParticleSystem ppp;
    private const string OutputDeviceName = "Microsoft GS Wavetable Synth";
    private IEnumerator coroutine;
    private OutputDevice _outputDevice;
    private Playback _playback;
    private List<NoteOn> noteOnList = new List<NoteOn>();
    [SerializeField] ParticleSystem myparticle = null;
    private float x_movement = 0.5f;
    private float y_movement = 0.5f;
    // Start is called before the first frame update
    void Start()
    {
        InitializeOutputDevice();
        var myFile = MidiFile.Read("Assets/MidiFiles/debussy-clair-de-lune.mid");
        //var myFile = MidiFile.Read("Assets/MidiFiles/thenights.mid");
        //var myFile = MidiFile.Read("Assets/MidiFiles/HotelCalifornia.mid");
        //var midiFile = CreateTestFile();
        InitializeFilePlayback(myFile);

        var tempoMap = myFile.GetTempoMap();
        IEnumerable<Note> notes = myFile.GetNotes();

        foreach (Note note in notes)
        {
            //Debug.Log(note.Velocity);
            
            var timeInMilliSeconds = ((TimeSpan)note.TimeAs<MetricTimeSpan>(tempoMap)).TotalMilliseconds;
            var lengthInSeconds = ((TimeSpan)note.LengthAs<MetricTimeSpan>(tempoMap)).TotalSeconds;
            //Debug.Log("notenum: " + note.NoteName + lengthInSeconds);
            Color c = GetRainbowColor2(note.NoteName.ToString(), (int)lengthInSeconds);
            NoteOn noteOn = new NoteOn(note.NoteName.ToString(), note.NoteNumber, (float)timeInMilliSeconds, (int)lengthInSeconds, note.Velocity ,c);
            noteOnList.Add(noteOn);
            
            //Debug.Log("r: " + c.r + " g:" + c.g + " b:" + c.b);
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
        transform.Translate(0, y_movement, 0);
        if (Mathf.Abs(transform.position.x) >= 8)
        {
            x_movement = -x_movement;
        }

        if (Mathf.Abs(transform.position.y) >= 2)
        {
            y_movement = -y_movement;
        }

        NoteOn firstNote = noteOnList.First();
        if (firstNote.Time <= (Time.time))
        {

            var main = myparticle.main;
            main.startColor = firstNote.color;
            main.startSpeed = (ParticleSystem.MinMaxCurve)(firstNote.Velocity * 0.1 * 0.5);

            myparticle.Play();
            noteOnList.Remove(firstNote);

            transform.Translate(x_movement, 0, 0);
            transform.Translate(0, y_movement, 0);
            if (Mathf.Abs(transform.position.x) >= 8)
            {
                x_movement = -x_movement;
            }

            if (Mathf.Abs(transform.position.y) >= 3)
            {
                y_movement = -y_movement;
            }
        }
    }


    public static Color GetRainbowColor(int pitch, int velocity)
    {

        int r = Math.Min(255, Math.Max(0, (108 - pitch) * (255 / 43)));
        int g = Math.Min(255, Math.Max(0, (pitch - 21) * (255 / 44)));
        int b = Math.Min(255, Math.Max(0, (pitch - 21) * (255 / 87)));

        // Adjust for whiteness
        r = (int)(r + (255 - r) * velocity / 100.0);
        g = (int)(g + (255 - g) * velocity / 100.0);
        b = (int)(b + (255 - b) * velocity / 100.0);
        return new Color(r / 255f, g / 255f, b / 255f);
    }

    public static Color GetRainbowColor2(string x, int velocity)
    {
        // Define a dictionary that maps each note to its corresponding pitch value
        Dictionary<string, double> pitchValues = new Dictionary<string, double>
    {
        { "C", 0 },
        { "CSharp", 1 },
        { "D", 2 },
        { "DSharp", 3 },
        { "E", 4 },
        { "ESharp", 5 },
        { "F", 5 },
        { "FSharp", 6 },
        { "G", 7 },
        { "GSharp", 8 },
        { "A", 9 },
        { "ASharp", 10 },
        { "B", 11 },
        { "BSharp", 12 }
    };

        // Get the pitch value of the given note
        double pitch = pitchValues[x];

        // Map the pitch value to a hue value in the range 0-360 degrees
        double hue = pitch / 12.0 * 360.0;

        // Convert the hue value to an RGB color
        return ColorFromHSL(hue, 1.0, 0.5, velocity);
    }

    // Helper function to convert HSL values to RGB color
    private static Color ColorFromHSL(double h, double s, double l, int velocity)
    {
        h /= 360.0;
        double r = 0, g = 0, b = 0;
        if (s == 0)
        {
            r = g = b = l;
        }
        else
        {
            double q = l < 0.5 ? l * (1 + s) : l + s - l * s;
            double p = 2 * l - q;
            r = HueToRGB(p, q, h + 1.0 / 3.0);
            g = HueToRGB(p, q, h);
            b = HueToRGB(p, q, h - 1.0 / 3.0);
        }

        //r = (int)(r + (255 - r) * velocity / 100.0);
        //g = (int)(g + (255 - g) * velocity / 100.0);
        //b = (int)(b + (255 - b) * velocity / 100.0);
        //return new Color((int)(255 * r), (int)(255 * g), (int)(255 * b));
        return new Color((int)(255 * r), (int)(255 * g), (int)(255 * b)) * 1.5f; ;
        //return Color.FromArgb((int)(255 * r), (int)(255 * g), (int)(255 * b));
    }

    // Helper function to convert hue value to RGB value
    private static double HueToRGB(double p, double q, double t)
    {
        if (t < 0) t += 1;
        if (t > 1) t -= 1;
        if (t < 1.0 / 6.0) return p + (q - p) * 6.0 * t;
        if (t < 1.0 / 2.0) return q;
        if (t < 2.0 / 3.0) return p + (q - p) * (2.0 / 3.0 - t) * 6.0;
        return p;
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

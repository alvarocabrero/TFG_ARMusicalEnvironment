using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Vuforia;

public class Audio_Manager
{
    private double startPlayingTime;

    private double nextEventTime;


    // Tiempo que dura una barra
    private double intervalo;
    private double loopLength;
    private static double BeatsperLoop = 1d / 16d;

    //key bpm, value audiosource
    public Dictionary<AudioSource, string> audioSourcesLoaded;

    public List<AudioSource> audioSourcesPlaying;
    private bool firstFrame;

    public Audio_Manager(float BPM)
    {
        intervalo = (60d / BPM);
        loopLength = intervalo * BeatsperLoop;
        startPlayingTime = 3;
        audioSourcesLoaded = new Dictionary<AudioSource, string>();
        audioSourcesPlaying = new List<AudioSource>();
        firstFrame = true;
    }


    public void SyncDemo()
    {
        startPlayingTime = AudioSettings.dspTime + 2d;
        foreach (var a in audioSourcesLoaded)
        {
            if (firstFrame)
            {
                a.Key.enabled = true;
                a.Key.PlayScheduled(startPlayingTime);
            }
            if (!a.Key.mute && !audioSourcesPlaying.Contains(a.Key))
            {
                audioSourcesPlaying.Add(a.Key);
            }
            else if (audioSourcesPlaying.Contains(a.Key) && a.Key.mute)
            {
                audioSourcesPlaying.Remove(a.Key);
            }
        }
        firstFrame = false;
    }
    public void Sync()
    {
        //Almacenar los audios que estan en play en una arraylist
        foreach (var a in audioSourcesLoaded)
        {
            if (a.Key.isActiveAndEnabled && !audioSourcesPlaying.Contains(a.Key))
            {
                audioSourcesPlaying.Add(a.Key);
            }
            else if (audioSourcesPlaying.Contains(a.Key) && !a.Key.isActiveAndEnabled)
            {
                audioSourcesPlaying.Remove(a.Key);
            }
        }
        // Sincronización de  loops
        if (audioSourcesPlaying.Count > 1)
        {

            //Loop siguiente a reproducir
            //
            for (int i = 1; i < audioSourcesPlaying.Count; i++)
            {
                if (!audioSourcesPlaying[i].isPlaying)
                {
                    nextEventTime = NextEvent();
                    //Schedule play at the offset
                    audioSourcesPlaying[i].PlayScheduled(nextEventTime);
                }
            }

        }
        else if (audioSourcesPlaying.Count == 1 && !audioSourcesPlaying[0].isPlaying)
        {
            // DSP DEVUELVE EL VALOR EN SEGUNDOS DEL SISTEMA DE AUDIO BASANDOSE EN EL SAMPLE RATE
            startPlayingTime = AudioSettings.dspTime + 2d;
            audioSourcesPlaying[0].PlayScheduled(startPlayingTime);
        }
    }

    double NextEvent()
    {
        double actualTime = AudioSettings.dspTime;
        double nTime = startPlayingTime + loopLength;

        if (actualTime < nTime)
        {
            return nTime;
        }
        else
        {
            while (nTime <= actualTime)
            {
                nTime += loopLength;
            }
            return nTime;

        }
    }

}

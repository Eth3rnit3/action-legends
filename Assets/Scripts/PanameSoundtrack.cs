using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        AudioSource musicOut,voiceOut;
        AudioClip musicClip,bossMusicClip;
        readonly AudioClip[] bonusVoices=new AudioClip[6];
        AudioClip bossVoice,winVoice;
        bool musicEnabled=true,voiceEnabled=true;
        float musicDuck;
        void LoadSoundtrack()
        {
            musicOut=gameObject.AddComponent<AudioSource>();musicOut.loop=true;musicOut.volume=.22f;
            voiceOut=gameObject.AddComponent<AudioSource>();voiceOut.volume=.95f;
            musicClip=Resources.Load<AudioClip>("Audio/cerises-arcade");bossMusicClip=Resources.Load<AudioClip>("Audio/cerises-boss");
            for(int i=0;i<6;i++)bonusVoices[i]=Resources.Load<AudioClip>("Audio/bonus-"+i);
            bossVoice=Resources.Load<AudioClip>("Audio/boss-alert");winVoice=Resources.Load<AudioClip>("Audio/victory");
        }
        void Speak(AudioClip clip)
        {
            if(!voiceEnabled||!clip)return;
            voiceOut.Stop();voiceOut.clip=clip;voiceOut.Play();musicDuck=clip.length+.15f;
        }
        void UpdateSoundtrack(float dt)
        {
            if(!musicOut)return;
            if(phase==Phase.Paused){if(musicOut.isPlaying)musicOut.Pause();if(voiceOut.isPlaying)voiceOut.Pause();return;}
            musicDuck=Mathf.Max(0,musicDuck-dt);
            if(phase==Phase.Playing)voiceOut.UnPause();
            AudioClip wanted=bossStarted&&bossMusicClip?bossMusicClip:musicClip;
            if(musicOut.clip!=wanted){musicOut.Stop();musicOut.clip=wanted;}
            bool shouldPlay=musicEnabled&&phase==Phase.Playing;
            if(shouldPlay&&!musicOut.isPlaying&&musicOut.clip){musicOut.UnPause();if(!musicOut.isPlaying)musicOut.Play();}
            if(!shouldPlay&&musicOut.isPlaying)musicOut.Pause();
            audioOut.volume=musicDuck>0?.085f:.2f;float target=musicDuck>0?.085f:.22f;
            musicOut.volume=Mathf.MoveTowards(musicOut.volume,target,dt*.5f);
            if(phase==Phase.Paused&&voiceOut.isPlaying)voiceOut.Pause();
        }
        void DrawSoundOptions()
        {
            if(Button(740,45,153,27,musicEnabled?"MUSIQUE : OUI":"MUSIQUE : NON",C("2A414E")))musicEnabled=!musicEnabled;
            if(Button(740,81,153,27,voiceEnabled?"VOIX : OUI":"VOIX : NON",C("2A414E"))){voiceEnabled=!voiceEnabled;if(!voiceEnabled)voiceOut.Stop();}
            Label(740,119,153,40,"LE TEMPS DES CERISES\nREMIX ARCADE",10,Gold,true,TextAnchor.MiddleCenter);
        }
    }
}

using System.Linq;
using UnityEngine;
using static Paname.PanameArt;
namespace Paname
{
    public sealed partial class PanameGame
    {
        AudioSource musicOut,voiceOut;
        AudioClip musicClip,bossMusicClip;
        // Three announcer takes per line; one is drawn at random each time it is spoken.
        AudioClip[] bossVoice,winVoice,impactClips,explosionClips;
        AudioClip pistolClip,machineGunClip,tripleClip,rocketClip,bossGunClip,playerHitClip,bossDownClip,pickupClip,coinClip,selectClip,startClip;
        // Cosmetic only: the simulation itself has no randomness, which keeps online peers in sync.
        readonly System.Random audioRandom=new System.Random();
        bool musicEnabled=true,voiceEnabled=true;
        float musicDuck;
        static AudioClip Sfx(string name)=>Resources.Load<AudioClip>("Audio/Sfx/"+name);
        static AudioClip[] Takes(string line)=>TakesAt("Audio/Voices/"+line);
        static AudioClip[] TakesAt(string path)=>new[]{"a","b","c"}.Select(take=>Resources.Load<AudioClip>(path+"-"+take)).Where(clip=>clip).ToArray();
        void LoadSoundtrack()
        {
            musicOut=gameObject.AddComponent<AudioSource>();musicOut.loop=true;musicOut.volume=.22f;
            voiceOut=gameObject.AddComponent<AudioSource>();voiceOut.volume=.95f;
            LoadMissionMusic();
            bossVoice=Takes("boss-alert");winVoice=Takes("victory");
            pistolClip=Sfx("tir-D-pistolet-sec");machineGunClip=Sfx("tir-A-mitraille");tripleClip=Sfx("tir-B-laser-snes");rocketClip=Sfx("roquette-depart");bossGunClip=Sfx("tir-C-blaster-cps1");
            impactClips=new[]{Sfx("impact-A-metal"),Sfx("impact-B-tchak"),Sfx("impact-C-ping")};
            explosionClips=new[]{Sfx("explosion-A-grosse"),Sfx("explosion-B-courte"),Sfx("explosion-C-bombe-neo")};
            playerHitClip=Sfx("joueur-touche");bossDownClip=Sfx("boss-explose");pickupClip=Sfx("bonus-ramasse");
            coinClip=Sfx("bonus-coin");selectClip=Sfx("menu-select");startClip=Sfx("menu-start");
        }
        void LoadMissionMusic()
        {
            musicClip=Resources.Load<AudioClip>(CurrentMission.music);bossMusicClip=Resources.Load<AudioClip>(CurrentMission.bossMusic);
        }
        AudioClip Pick(AudioClip[] clips)=>clips==null||clips.Length==0?null:clips[audioRandom.Next(clips.Length)];
        void Sound(AudioClip[] clips)=>Sound(Pick(clips));
        AudioClip FireClip(int weapon)=>weapon==1?machineGunClip:weapon==2?tripleClip:weapon==3?rocketClip:pistolClip;
        void Speak(AudioClip[] takes)
        {
            var clip=Pick(takes);
            if(!voiceEnabled||!clip||!voiceOut)return;
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
            if(Button(740,40,153,25,musicEnabled?"MUSIQUE : OUI":"MUSIQUE : NON",C("2A414E")))musicEnabled=!musicEnabled;
            if(Button(740,69,153,25,voiceEnabled?"VOIX : OUI":"VOIX : NON",C("2A414E"))){voiceEnabled=!voiceEnabled;if(!voiceEnabled)voiceOut.Stop();}
        }
    }
}

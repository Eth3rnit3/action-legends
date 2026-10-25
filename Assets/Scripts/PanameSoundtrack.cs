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
        readonly AudioClip[][] bonusVoices=new AudioClip[6][];
        AudioClip[] bossVoice,winVoice,impactClips,explosionClips;
        AudioClip pistolClip,machineGunClip,tripleClip,rocketClip,bossGunClip,playerHitClip,bossDownClip,pickupClip,coinClip,selectClip,startClip;
        // Cosmetic only: never draws from the seeded simulation generator, so online peers stay in sync.
        readonly System.Random audioRandom=new System.Random();
        bool musicEnabled=true,voiceEnabled=true;
        float musicDuck;
        static AudioClip Sfx(string name)=>Resources.Load<AudioClip>("Audio/Sfx/"+name);
        static AudioClip[] Takes(string line)=>new[]{"a","b","c"}.Select(take=>Resources.Load<AudioClip>("Audio/Voices/"+line+"-"+take)).Where(clip=>clip).ToArray();
        void LoadSoundtrack()
        {
            musicOut=gameObject.AddComponent<AudioSource>();musicOut.loop=true;musicOut.volume=.22f;
            voiceOut=gameObject.AddComponent<AudioSource>();voiceOut.volume=.95f;
            musicClip=Resources.Load<AudioClip>("Audio/Music/marseillaise-arcade");bossMusicClip=Resources.Load<AudioClip>("Audio/Music/marseillaise-boss");
            for(int i=0;i<6;i++)bonusVoices[i]=Takes("bonus-"+i);
            bossVoice=Takes("boss-alert");winVoice=Takes("victory");
            pistolClip=Sfx("tir-D-pistolet-sec");machineGunClip=Sfx("tir-A-mitraille");tripleClip=Sfx("tir-B-laser-snes");rocketClip=Sfx("roquette-depart");bossGunClip=Sfx("tir-C-blaster-cps1");
            impactClips=new[]{Sfx("impact-A-metal"),Sfx("impact-B-tchak"),Sfx("impact-C-ping")};
            explosionClips=new[]{Sfx("explosion-A-grosse"),Sfx("explosion-B-courte"),Sfx("explosion-C-bombe-neo")};
            playerHitClip=Sfx("joueur-touche");bossDownClip=Sfx("boss-explose");pickupClip=Sfx("bonus-ramasse");
            coinClip=Sfx("bonus-coin");selectClip=Sfx("menu-select");startClip=Sfx("menu-start");
        }
        AudioClip Pick(AudioClip[] clips)=>clips==null||clips.Length==0?null:clips[audioRandom.Next(clips.Length)];
        void Sound(AudioClip[] clips)=>Sound(Pick(clips));
        AudioClip FireClip(int weapon)=>weapon==1?machineGunClip:weapon==2?tripleClip:weapon==3?rocketClip:pistolClip;
        void Speak(AudioClip[] takes)
        {
            var clip=Pick(takes);
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
            Label(740,119,153,40,"LA MARSEILLAISE\nREMIX ARCADE FM",10,Gold,true,TextAnchor.MiddleCenter);
        }
    }
}

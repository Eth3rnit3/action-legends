using System;
using UnityEngine;
namespace Paname
{
    public static class ArcadeAudio
    {
        // Layered transient, noisy blast and low body replace the prototype's simple sine tones.
        public static AudioClip Create(string name,int kind)
        {
            const int rate=22050;float length=kind==0?.17f:kind==1?.16f:.52f;
            var samples=new float[(int)(length*rate)];var random=new System.Random(70+kind);float low=0,phase=0,peak=.001f;
            for(int i=0;i<samples.Length;i++)
            {
                float t=(float)i/rate,noise=(float)random.NextDouble()*2-1;low=low*.75f+noise*.25f;
                phase+=Mathf.PI*2*(kind==2?Mathf.Lerp(95,38,t/length):Mathf.Lerp(145,58,t/length))/rate;
                float s;
                if(kind==0)s=noise*.55f*Mathf.Exp(-t*65)+low*.65f*Mathf.Exp(-t*28)+Mathf.Sin(phase)*.65f*Mathf.Exp(-t*38)+noise*.4f*Mathf.Exp(-t*650);
                else if(kind==1)s=(Mathf.Sin(t*1400*6.283f)*.23f+Mathf.Sin(t*2371*6.283f)*.18f+Mathf.Sin(t*3107*6.283f)*.1f)*Mathf.Exp(-t*43)+noise*.65f*Mathf.Exp(-t*100);
                else s=low*1.3f*Mathf.Exp(-t*8)+Mathf.Sin(phase)*.75f*Mathf.Exp(-t*11)+noise*.45f*Mathf.Exp(-t*75);
                s*=Mathf.Clamp01(t/.0007f);samples[i]=s;peak=Mathf.Max(peak,Mathf.Abs(s));
            }
            for(int i=0;i<samples.Length;i++)samples[i]=samples[i]/peak*.9f;
            var clip=AudioClip.Create(name,samples.Length,1,rate,false);clip.SetData(samples,0);return clip;
        }
    }
}

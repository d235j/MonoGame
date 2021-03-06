#region License
// /*
// Microsoft Public License (Ms-PL)
// MonoGame - Copyright © 2009 The MonoGame Team
// 
// All rights reserved.
// 
// This license governs use of the accompanying software. If you use the software, you accept this license. If you do not
// accept the license, do not use the software.
// 
// 1. Definitions
// The terms "reproduce," "reproduction," "derivative works," and "distribution" have the same meaning here as under
// U.S. copyright law.
// 
// A "contribution" is the original software, or any additions or changes to the software.
// A "contributor" is any person that distributes its contribution under this license.
// "Licensed patents" are a contributor's patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// (A) Copyright Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free copyright license to reproduce its contribution, prepare derivative works of its contribution, and distribute its contribution or any derivative works that you create.
// (B) Patent Grant- Subject to the terms of this license, including the license conditions and limitations in section 3, 
// each contributor grants you a non-exclusive, worldwide, royalty-free license under its licensed patents to make, have made, use, sell, offer for sale, import, and/or otherwise dispose of its contribution in the software or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// (A) No Trademark License- This license does not grant you rights to use any contributors' name, logo, or trademarks.
// (B) If you bring a patent claim against any contributor over patents that you claim are infringed by the software, 
// your patent license from such contributor to the software ends automatically.
// (C) If you distribute any portion of the software, you must retain all copyright, patent, trademark, and attribution 
// notices that are present in the software.
// (D) If you distribute any portion of the software in source code form, you may do so only under this license by including 
// a complete copy of this license with your distribution. If you distribute any portion of the software in compiled or object 
// code form, you may only do so under a license that complies with this license.
// (E) The software is licensed "as-is." You bear the risk of using it. The contributors give no express warranties, guarantees
// or conditions. You may have additional consumer rights under your local laws which this license cannot change. To the extent
// permitted under your local laws, the contributors exclude the implied warranties of merchantability, fitness for a particular
// purpose and non-infringement.
// */
#endregion License

#region Using Statements
using System;

using OpenTK.Audio.OpenAL;

using Microsoft.Xna.Framework;

#endregion Statements

namespace Microsoft.Xna.Framework.Audio
{
    /// <summary>
    /// Implements the SoundEffectInstance, which is used to access high level features of a SoundEffect. This class uses the OpenAL
    /// sound system to play and control the sound effects. Please refer to the OpenAL 1.x specification from Creative Labs to better
    /// understand the features provides by SoundEffectInstance. 
    /// </summary>
	public class SoundEffectInstance : IDisposable
	{
		private bool isDisposed = false;
		private SoundState soundState = SoundState.Stopped;
		private OALSoundBuffer[] soundBuffer;
		private OpenALSoundController controller;
		private SoundEffect soundEffect;

		private Vector3 position = new Vector3(0.0f, 0.0f, 0.1f);
		private Vector3 velocity = new Vector3(0.0f, 0.0f, 0.0f);

		// Used to prevent outdated positional audio data from being used
		private bool positionalAudio;

		private float _volume = 1.0f;
		private bool _looped = false;
		private float _pan = 0f;
		private float _pitch = 0f;
        
		private int loopStart;
		private int loopEnd;

		bool hasSourceId = false;
		int sourceId;

        /// <summary>
        /// Creates an instance and initializes it.
        /// </summary>
        public SoundEffectInstance()
        {
            InitializeSound(1);
        }

        ~SoundEffectInstance()
        {
            Dispose();
        }
  
        /* Creates a standalone SoundEffectInstance from given wavedata. */
        internal SoundEffectInstance(byte[] buffer, int sampleRate, int channels)
        {
            InitializeSound(1);
            soundBuffer[0].BindDataBuffer(
                buffer,
                (channels == 2) ? ALFormat.Stereo16 : ALFormat.Mono16,
                buffer.Length,
                sampleRate
            );
            loopStart = 0;
            loopEnd = buffer.Length;
        }
        
        /// <summary>
        /// Construct the instance from the given SoundEffect. The data buffer from the SoundEffect is 
        /// preserved in this instance as a reference. This constructor will bind the buffer in OpenAL.
        /// </summary>
        /// <param name="parent"></param>
		public SoundEffectInstance (SoundEffect parent)
		{
            loopStart = parent.loopStart;
            loopEnd = parent.loopEnd;
            int channels = (parent.Format == ALFormat.Stereo16) ? 2 : 1;
            int numBuffers = 1;
            if (parent.loopStart > 0)
            {
                numBuffers++;
            }
            if (parent.loopEnd < (parent._data.Length / 2 / channels))
            {
                numBuffers++;
            }
			InitializeSound(numBuffers);
            if (numBuffers == 3)
            {
                soundBuffer[0].BindDataBuffer(
                    parent._data,
                    parent.Format,
                    loopStart * 2 * channels,
                    (int) parent.Rate
                );
                byte[] midBuf = new byte[(loopEnd * 2 * channels) - (loopStart * 2 * channels)];
                int cur = 0;
                for (int i = (loopStart * 2 * channels); i < loopEnd * 2 * channels; i++)
                {
                    midBuf[cur] = parent._data[i];
                    cur++;
                }
                soundBuffer[1].BindDataBuffer(
                    midBuf,
                    parent.Format,
                    midBuf.Length,
                    (int) parent.Rate
                );
                midBuf = new byte[parent._data.Length - (loopEnd * 2 * channels)];
                cur = 0;
                for (int i = (loopEnd * 2 * channels); i < parent._data.Length; i++)
                {
                    midBuf[cur] = parent._data[i];
                    cur++;
                }
                soundBuffer[2].BindDataBuffer(
                    midBuf,
                    parent.Format,
                    midBuf.Length,
                    (int) parent.Rate
                );
            }
            else if (numBuffers == 2)
            {
                soundBuffer[0].BindDataBuffer(
                    parent._data,
                    parent.Format,
                    loopStart * 2 * channels,
                    (int) parent.Rate
                );
                byte[] midBuf = new byte[(loopEnd * 2 * channels) - (loopStart * 2 * channels)];
                int cur = 0;
                for (int i = loopStart * 2 * channels; i < (loopEnd * 2 * channels); i++)
                {
                    midBuf[cur] = parent._data[i];
                    cur++;
                }
                soundBuffer[1].BindDataBuffer(
                    midBuf,
                    parent.Format,
                    midBuf.Length,
                    (int) parent.Rate
                );
            }
            else
            {
                soundBuffer[0].BindDataBuffer(
                    parent._data,
                    parent.Format,
                    parent._data.Length,
                    (int) parent.Rate
                );
            }
		}

        /// <summary>
        /// Gets the OpenAL sound controller, constructs the sound buffer, and sets up the event delegates for
        /// the reserved and recycled events.
        /// </summary>
		private void InitializeSound(int numBuffers)
		{
			controller = OpenALSoundController.GetInstance;
			soundBuffer = new OALSoundBuffer[numBuffers];
            for (int i = 0; i < numBuffers; i++)
            {
                soundBuffer[i] = new OALSoundBuffer();
                soundBuffer[i].Reserved += HandleSoundBufferReserved;
                soundBuffer[i].Recycled += HandleSoundBufferRecycled;
            }

			positionalAudio = false;
		}

        /// <summary>
        /// Event handler that resets internal state of this instance. The sound state will report
        /// SoundState.Stopped after this event handler.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void HandleSoundBufferRecycled (object sender, EventArgs e)
		{
			sourceId = 0;
			hasSourceId = false;
			soundState = SoundState.Stopped;
			//Console.WriteLine ("recycled: " + soundEffect.Name);
		}

        /// <summary>
        /// Called after the hardware has allocated a sound buffer, this event handler will
        /// maintain the numberical ID of the source ID.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
		private void HandleSoundBufferReserved (object sender, EventArgs e)
		{
			sourceId = soundBuffer[0].SourceId;
			hasSourceId = true;
		}

        /// <summary>
        /// Stops the current running sound effect, if relevant, removes its event handlers, and disposes
        /// of the sound buffer.
        /// </summary>
		public void Dispose ()
        {
            if (!isDisposed)
            {
                this.Stop(true);
                for (int i = 0; i < soundBuffer.Length; i++)
                {
                    soundBuffer[i].Reserved -= HandleSoundBufferReserved;
                    soundBuffer[i].Recycled -= HandleSoundBufferRecycled;
                    soundBuffer[i].Dispose();
                    soundBuffer[i] = null;
                }
                if (controller.loopingInstances.Contains(this))
                {
                    controller.loopingInstances.Remove(this);
                }
                isDisposed = true;
            }
		}
		
        /// <summary>
        /// Wrapper for Apply3D(AudioListener[], AudioEmitter)
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="emitter"></param>
		public void Apply3D (AudioListener listener, AudioEmitter emitter)
		{
			Apply3D ( new AudioListener[] { listener }, emitter);
		}
		
        /// <summary>
        /// Applies a 3D transform on the emitter and the listeners to account for head-up
        /// listening orientation in a 3D surround-sound pseudo-environment. The actual 3D
        /// sound production is handled by OpenAL. This method computes the listener positions
        /// and orientation and hands off the calculations to OpenAL.
        /// </summary>
        /// <param name="listeners"></param>
        /// <param name="emitter"></param>
		public void Apply3D (AudioListener[] listeners, AudioEmitter emitter)
		{
			// get AL's listener position
			float x, y, z;
			AL.GetListener (ALListener3f.Position, out x, out y, out z);

			for (int i = 0; i < listeners.Length; i++)
			{
				AudioListener listener = listeners[i];
				
				// get the emitter offset from origin
				Vector3 posOffset = emitter.Position - listener.Position;
				// set up orientation matrix
				Matrix orientation = Matrix.CreateWorld(Vector3.Zero, listener.Forward, listener.Up);
				// set up our final position and velocity according to orientation of listener
				position = new Vector3(x + posOffset.X, y + posOffset.Y, z + posOffset.Z);
				position = Vector3.Transform(position, orientation);
				velocity = emitter.Velocity;
				velocity = Vector3.Transform(velocity, orientation);

				// FIXME: This is totally arbitrary. I dunno the exact ratio here.
				position /= 255.0f;
				velocity /= 255.0f;
				
				// set the position based on relative positon
				AL.Source(sourceId, ALSource3f.Position, position.X, position.Y, position.Z);
				AL.Source(sourceId, ALSource3f.Velocity, velocity.X, velocity.Y, velocity.Z);
			}

			positionalAudio = true;
		}

        /// <summary>
        /// When the sound state is playing and the source is created, this method will pause
        /// the sound playback and set the state to SoundState.Paused. Otherwise, no change is
        /// made to the state of this instance.
        /// </summary>
		public void Pause ()
		{
			if (hasSourceId && soundState == SoundState.Playing)
            {
				controller.PauseSound(soundBuffer[0]);
				soundState = SoundState.Paused;
			}
		}

		/// <summary>
		/// Converts the XNA [-1,1] pitch range to OpenAL (-1,+INF].
        /// <param name="xnaPitch">The pitch of the sound in the Microsoft XNA range.</param>
		/// </summary>
        private float XnaPitchToAlPitch(float xnaPitch)
        {
            /* 
            XNA sets pitch bounds to [-1.0f, 1.0f], each end being one octave.
             •OpenAL's AL_PITCH boundaries are (0.0f, INF). *
             •Consider the function f(x) = 2 ^ x
             •The domain is (-INF, INF) and the range is (0, INF). *
             •0.0f is the original pitch for XNA, 1.0f is the original pitch for OpenAL.
             •Note that f(0) = 1, f(1) = 2, f(-1) = 0.5, and so on.
             •XNA's pitch values are on the domain, OpenAL's are on the range.
             •Remember: the XNA limit is arbitrarily between two octaves on the domain. *
             •To convert, we just plug XNA pitch into f(x). 
                    */
            if (xnaPitch < -1.0f || xnaPitch > 1.0f)
            {
                throw new Exception("XNA PITCH MUST BE WITHIN [-1.0f, 1.0f]!");
            }
            return (float)Math.Pow(2, xnaPitch);
        }

        /// <summary>
        /// Sends the position, gain, looping, pitch, and distance model to the OpenAL driver.
        /// </summary>
		private void ApplyState ()
		{
			if (!hasSourceId)
				return;
			// Distance Model
			AL.DistanceModel (ALDistanceModel.InverseDistanceClamped);
			// Listener
			// Pan/Position
			if (positionalAudio)
			{
				positionalAudio = false;
				AL.Source(sourceId, ALSource3f.Position, position.X, position.Y, position.Z);
				AL.Source(sourceId, ALSource3f.Velocity, velocity.X, velocity.Y, velocity.Z);
			}
			else
			{
				AL.Source (sourceId, ALSource3f.Position, _pan, 0, 0.1f);
			}
			// Volume
			AL.Source (sourceId, ALSourcef.Gain, _volume * SoundEffect.MasterVolume);
			// Looping
			IsLooped = IsLooped; // This looks stupid, I know. But trust me. -flibit
			// Pitch
			AL.Source (sourceId, ALSourcef.Pitch, XnaPitchToAlPitch(_pitch));
		}

        /// <summary>
        /// If no source is ready, then this method does not change the current state of the instance. Otherwise,
        /// if the controller can not reserve the source then InstancePLayLimitException is thrown. Finally, the sound
        /// buffer is sourced to OpenAL, then ApplyState is called and then the sound is set to play. Upon success,
        /// the sound state is set to SoundState.Playing.
        /// </summary>
		public virtual void Play ()
		{
			if (hasSourceId) {
				return;
			}
			bool isSourceAvailable = controller.ReserveSource(soundBuffer[0]);
			if (!isSourceAvailable)
			{
				System.Console.WriteLine("WARNING: AL SOURCE WAS NOT AVAILABLE. SKIPPING.");
				return;
				//throw new InstancePlayLimitException();
			}
   
            if (soundBuffer.Length > 1)
            {
                for (int i = 0; i < soundBuffer.Length; i++)
                {
                    if (IsLooped && i == 2)
                    {
                        break; // FIXME: God help you if you Loop during playback.
                    }
                    AL.SourceQueueBuffer(
                        soundBuffer[0].SourceId,
                        soundBuffer[i].OpenALDataBuffer
                    );
                }
                triggeredDequeue = false;
            }
            else
            {
                int bufferId = soundBuffer[0].OpenALDataBuffer;
                AL.Source(soundBuffer[0].SourceId, ALSourcei.Buffer, bufferId);
            }
			ApplyState();

			controller.PlaySound(soundBuffer[0]);            
			//Console.WriteLine ("playing: " + sourceId + " : " + soundEffect.Name);
			soundState = SoundState.Playing;
		}

        /// <summary>
        /// When the sound state is paused, and the source is available, then the sound
        /// is played using the ResumeSound method from the OpenALSoundController. Otherwise,
        /// the sound is played using the Play() method. Upon success, the sound state should
        /// be SoundState.Playing.
        /// </summary>
		public void Resume ()
		{
            if (hasSourceId)
            {
                if (soundState == SoundState.Paused)
                {
                    controller.ResumeSound(soundBuffer[0]);
                    soundState = SoundState.Playing;
                }
            }
            else
            {
                /* We cannot assume that Resume is the same thing as Play.
                 * Resume should only work in cooperation with Pause!
                 * -flibit
                 */
                // Play();
            }
		}

        /// <summary>
        /// When the source is available, the sound buffer playback is stopped. Either way,
        /// the state of the instance will always be SoundState.Stopped after this method is
        /// called.
        /// </summary>
		public void Stop ()
		{
			if (hasSourceId) {
				//Console.WriteLine ("stop " + sourceId + " : " + soundEffect.Name);
				controller.StopSound(soundBuffer[0]);
			}
			soundState = SoundState.Stopped;
		}

        /// <summary>
        /// Wrapper for Stop()
        /// </summary>
        /// <param name="immediate">Is not used.</param>
		public void Stop (bool immediate)
		{
			Stop ();
		}
        
        bool triggeredDequeue = false;
        internal void checkLoop()
        {
            int processed;
            AL.GetSource(soundBuffer[0].SourceId, ALGetSourcei.BuffersProcessed, out processed);
            if (!triggeredDequeue && processed > 0)
            {
                AL.SourceUnqueueBuffer(soundBuffer[0].SourceId);
                triggeredDequeue = true;
                AL.Source(soundBuffer[0].SourceId, ALSourceb.Looping, true);
            }
        }

        /// <summary>
        /// returns true if this object has been disposed.
        /// </summary>
		public bool IsDisposed {
			get {
				return isDisposed;
			}
		}

        /// <summary>
        /// Set/get if this sound is looped. When set, and the source is already active, then
        /// the looping setting is applied immediately.
        /// </summary>
		public virtual bool IsLooped {
			get {
				return _looped;
			}

			set {
				_looped = value;
				if (_looped && !controller.loopingInstances.Contains(this))
				{
					controller.loopingInstances.Add(this);
				}
				else if (!_looped && controller.loopingInstances.Contains(this))
				{
					controller.loopingInstances.Remove(this);
				}
				if (hasSourceId) {
					// Looping
					AL.Source (sourceId, ALSourceb.Looping, _looped && soundBuffer.Length == 1);
				}
			}
		}

        /// <summary>
        /// Set/get for sound panning. Sound panning controls the location of the listener in the coordinate space
        /// defined by your world. This method only affects the 'x' coordinate of the listener. The final position of
        /// the listener is (pan, 0, 0.1). 
        /// </summary>
		public float Pan {
			get {
				return _pan;
			}

			set {
				_pan = value;
				if (hasSourceId) {
					// Listener
					// Pan
					AL.Source (sourceId, ALSource3f.Position, _pan, 0.0f, 0.1f);
				}
			}
		}

        /// <summary>
        /// Set/get the pitch (Octave adjustment) of the sound effect. This attribute assumes you are setting
        /// the pitch using the [-1,1] Microsoft XNA pitch range. The pitch will be automatically adjusted
        /// using the XnaPitchToAlPitch method. If the source is active, then the pitch change will
        /// be applied immediately.
        /// </summary>
		public float Pitch {
			get {
				return _pitch;
			}
			set {
				_pitch = value;
				if (hasSourceId) {
					// Pitch
					AL.Source (sourceId, ALSourcef.Pitch, XnaPitchToAlPitch(_pitch));
				}

			}
		}

        /// <summary>
        /// Returns the current state of the SoundEffect.
        /// </summary>
		public SoundState State {
			get {
				return soundState;
			}
		}

        /// <summary>
        /// Get/set the relative volume of this sound effect. The volume is relative to the master
        /// volume (SoundEffect.MasterVolume). The values in this attribute should be [0,1]. If the source
        /// is active, then volume changes will be applied immediately.
        /// </summary>
		public float Volume {
			get {
				return _volume;
			}
			
			set {
				_volume = value;
				if (hasSourceId) {
					// Volume
					AL.Source (sourceId, ALSourcef.Gain, _volume * SoundEffect.MasterVolume);
				}

			}
		}	
		
		
	}
}

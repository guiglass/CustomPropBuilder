//======= Copyright (c) Valve Corporation, All rights reserved. ===============
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System;

namespace Valve.VR.InteractionSystem.Sample
{
    public class ButtonToggle : MonoBehaviour
    {

		public HoverButton hoverButton;

		public bool toggleState;

		public AudioClip turnOnClip;

		public AudioClip turnOffClip;

		public GameObject[] objects;

		public GameObject[] objectsOppisite;

		private void Start()
		{
			//hoverButton.onButtonDown.AddListener(OnButtonDown);
		}

		private void OnButtonDown(Hand hand)
		{
			toggleState = !toggleState;

			foreach (var obj in objects) {
				obj.SetActive (toggleState);
			}
			foreach (var obj in objectsOppisite) {
				obj.SetActive (!toggleState);
			}

			var clip = toggleState ? turnOnClip : turnOffClip;
			if (clip != null) {
				PlayOneshotAudio (clip);
			}

		}


		public static AudioSource PlayOneshotAudio(AudioClip clip, float volume = 1f, float pitch = 1f, bool play = true, bool bypassEffects = false) {
			var obj = new GameObject ();
			obj.name = "oneshot_audio";
			var oneshotAudio = obj.AddComponent<AudioSource> ();
			oneshotAudio.clip = clip;
			oneshotAudio.volume = volume;
			oneshotAudio.pitch = pitch;

			oneshotAudio.bypassReverbZones = bypassEffects;
			oneshotAudio.bypassListenerEffects = bypassEffects;
			oneshotAudio.bypassEffects = bypassEffects;

			if (play) {
				oneshotAudio.Play ();
			}
			Destroy(oneshotAudio.gameObject, clip.length * (1f/pitch));
			return oneshotAudio;
		}
    }
}
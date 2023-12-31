﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Random;
using static UnityEngine.Debug;

public class DevilishCalculationsScript : MonoBehaviour {

	public KMBombInfo Bomb;
	public KMAudio Audio;
	public KMNeedyModule Needy;

	public KMSelectable[] keypadButtons;

	public TextMesh[] displays;

	static int moduleIdCounter = 1;
	int moduleId;
	private bool needyDeactivated = true;
	private bool activatedOnce;

	private int backIx;
	private List<int> answers = new List<int>();
	private List<string> expressions = new List<string>();

	private int ExpressionAnswer(int a, int b, bool addition) => addition ? a + b : Math.Max(a, b) - Math.Min(a, b);

	class DevilishCalculationsSettings
	{
		public int Back = 2;
	}

	private DevilishCalculationsSettings DevilishCalcSettings = new DevilishCalculationsSettings();

	void Awake()
    {

        ModConfig<DevilishCalculationsSettings> config = new ModConfig<DevilishCalculationsSettings>("DevilishCalculationsSettings");
		DevilishCalcSettings = config.Settings;
		config.Settings = DevilishCalcSettings;
		backIx = DevilishCalcSettings.Back < 1 || DevilishCalcSettings.Back > 3 ? 2 : DevilishCalcSettings.Back;

        moduleId = moduleIdCounter++;

		foreach (KMSelectable key in keypadButtons)
			key.OnInteract += delegate () { KeypadPress(key); return false; };

		Needy.OnNeedyActivation += NeedyActivation;
		Needy.OnNeedyDeactivation += NeedyDeactivation;
		Needy.OnTimerExpired += NeedyTimerExpired;

	}


	void Start()
    {
		Log($"[Devilish Calculations #{moduleId}] The needy is configured to {backIx}-Back.");
    }

	void KeypadPress(KMSelectable key)
	{
		key.AddInteractionPunch(0.4f);

		if (needyDeactivated || answers.Count <= backIx)
		{
			Audio.PlaySoundAtTransform("NoInteraction", transform);
			return;
		}
		
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);

		switch (Array.IndexOf(keypadButtons, key))
		{
			case 7:
				displays[1].text = string.Empty;
				break;
			case 11:
				if (int.Parse(displays[1].text) == answers[backIx == 1 ? answers.Count - backIx : answers.Count - backIx - 1])
				{
					Log($"[Devilish Calculations #{moduleId}] Expected input is correct.");
					needyDeactivated = true;

					foreach (var text in displays)
						text.text = "";

					needyDeactivated = true;
                    answers.RemoveAt(0);
                    expressions.RemoveAt(0);
					NeedyDeactivation();
                }
				else
				{
					var length = displays[1].text.Length == 0 ? "nothing" : displays[1].text;
					Log($"[Devilish Calculations #{moduleId}] Expected {answers[backIx == 1 ? answers.Count - backIx : answers.Count - backIx - 1]}, but inputted {length}. Strike!");
					Needy.HandleStrike();
					NeedyDeactivation();
				}
				break;
			default:
				if (displays[1].text.Length < 2)
					displays[1].text += key.GetComponentInChildren<TextMesh>().text;
				break;
		}
	}

	void MathGeneration()
	{
		var addition = Range(0, 2) == 0;
		int a = Range(0, 10), b = Range(0, 10);
		answers.Add(ExpressionAnswer(a, b, addition));
		expressions.Add(addition ? $"{a}+{b}" : $"{Math.Max(a, b)}-{Math.Min(a, b)}");
	}

	void NeedyActivation()
	{
		needyDeactivated = false;
		MathGeneration();
		if (!activatedOnce)
			activatedOnce = true;

		Log($"[Devilish Calculations # {moduleId}] {expressions.Last()} = {answers.Last()}");

		if (answers.Count > backIx)
		{
			var variation = backIx == 1 ? "activation" : "activations";
			Audio.PlaySoundAtTransform("Fanfare", transform);
			Log($"[Devilish Calculations #{moduleId}] The input needed from {backIx} {variation} ago is {answers[backIx == 1 ? answers.Count - backIx : answers.Count - backIx - 1]}");
		}

		displays[0].text = expressions.Last();
	}

	void NeedyDeactivation()
	{
		foreach (var text in displays)
			text.text = string.Empty;
		needyDeactivated = true;
		Needy.HandlePass();
	}

	void NeedyTimerExpired()
	{
		if (answers.Count > backIx)
		{
			Log($"[Devilish Calculations #{moduleId}] The timer has ran out and no input has been made. Strike!");

			Needy.HandleStrike();
			answers.RemoveAt(0);
			expressions.RemoveAt(0);
        }

        foreach (var text in displays)
            text.text = string.Empty;
    }

	// Twitch Plays

	int GetNumIndex(char c) => "1230456A789B".IndexOf(c);

#pragma warning disable 414
	private readonly string TwitchHelpMessage = @"!{0} submit 0123546789 to input your answer. || !{0} config to see what the needy's back index is configured to. || !{0} set 123 to set the needy's back index to that number. Keep in mind you cannot change this once the needy is activated the first time.";
#pragma warning restore 414

	IEnumerator ProcessTwitchCommand (string command)
    {
		string[] split = command.ToUpperInvariant().Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
		yield return null;

		if (split[0].EqualsIgnoreCase("CONFIG"))
		{
			yield return $"sendtochat This needy is configured to {backIx}-Back.";
			yield break;
		}

		if (split[0].EqualsIgnoreCase("SET"))
		{
			if (activatedOnce)
			{
				yield return "sendtochaterror Setting back configuration is no longer possible due to the needy already activated once or more.";
				yield break;
			}
			if (!"123".Contains(split[1]))
				yield break;

			backIx = int.Parse(split[1]);
			yield return $"sendtochat The needy is now configured to {backIx}-Back.";
			Log($"[Devilish Calculations #{moduleId}] Twitch Plays has now set the needy configuration to {backIx}-Back.");
			yield break;
		}

		if (needyDeactivated)
		{
			yield return "sendtochaterror The needy is not activated yet!";
			yield break;
		}
		else if (answers.Count < backIx)
		{
			yield return "sendtochaterror The needy is not ready for submission yet!";
			yield break;
		}

		if (split[0].EqualsIgnoreCase("SUBMIT"))
		{
			if (split.Length == 1)
			{
				yield return "sendtochaterror Please specify either a single or double digit number!";
				yield break;
			}
			if (split[1].Length > 2)
			{
				yield return "sendtochaterror You cannot input more than 2 numbers!";
				yield break;
			}

			var input = split[1].Select(GetNumIndex).ToArray();

			if (input.Any(x => x < 0 || x == 7 || x == 11))
				yield break;

			if (displays[1].text.Length != 0)
			{
				keypadButtons[7].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			foreach (var num in input)
			{
				keypadButtons[num].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			keypadButtons[11].OnInteract();
		}

    }

	void TwitchHandleForcedSolve()
    {
		StartCoroutine(needyAutosolve());
    }

	IEnumerator needyAutosolve()
	{
		while (true)
		{
			while (needyDeactivated || answers.Count <= backIx)
				yield return null;


			if (displays[1].text.Length != 0)
			{
				if (int.Parse(displays[1].text) == answers[backIx == 1 ? answers.Count - backIx : answers.Count - backIx - 1])
				{
					keypadButtons[11].OnInteract();
					yield return new WaitForSeconds(0.1f);
					continue;
				}

                keypadButtons[7].OnInteract();
                yield return new WaitForSeconds(0.1f);
            }

			var answer = answers[backIx == 1 ? answers.Count - backIx : answers.Count - backIx - 1].ToString().Select(GetNumIndex).ToArray();

			foreach (var num in answer)
			{
				keypadButtons[num].OnInteract();
				yield return new WaitForSeconds(0.1f);
			}

			keypadButtons[11].OnInteract();
			yield return new WaitForSeconds(0.1f);
		}
	}


}






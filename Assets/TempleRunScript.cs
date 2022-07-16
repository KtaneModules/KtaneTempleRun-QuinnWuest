using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using Rnd = UnityEngine.Random;

public class TempleRunScript : MonoBehaviour
{
    public KMNeedyModule Needy;
    public KMBombInfo BombInfo;
    public KMAudio Audio;

    public TextMesh TopText;
    public TextMesh BottomText;

    private int _moduleId;
    private static int _moduleIdCounter = 1;

    private static readonly string[] _responses = new string[] { "SWING ON", "JUMP OVER", "CLIMB", "DUCK UNDER", "STOMP ON", "DODGE", "CHOP", "RUN FROM", "SWIM THROUGH", "GRAB", "LEAP OVER", "BLOW ON", "BOO", "TRAP", "QUACK AT", "SWAY AROUND", "TREAD ON", "CRUSH", "HISS AT", "AHH AT", "SAY FRUIT AT", "UNDERMINE", "PASS THROUGH", "GO MMM AT", "OWL", "BIG", "CONSTRUCT", "PAY", "SUBSCRIBE TO", "SWAP WITH", "CRSUH", "SPONGE", "HUG", "SUPER", "CRSHU", "RCSHU", "HSURC", "JUJU ON", "VQCPEDHCU", "WAH", "QUOTE", "COPY", "COPE WITH", "BAN", "OWN", "NUKE" };
    private static readonly string[] _calls = new string[] { "VINE", "PIT", "WALL", "ARROW", "SPIDER", "SPEAR", "ZOMBIE", "BOULDER", "RIVER", "LOOT", "TRIPWIRE", "FIRE", "GHOST", "MOLE", "DUCK", "CROSSBOW", "SPIKES", "SCORPION", "SNAKES", "SPIRIT", "PEAR", "VOLE", "HALL", "TOAST", "HOOT", "CHUNGUS", "PYLONS", "CHILD SUPPORT", "BRAMBLEGAMING", "SHAUN", "SCOPRION", "TECHNO", "TAHMIS", "MARIO", "SCORPOIN", "SCOPROIN", "NIOPROCS", "BEAT", "KPWRAQNTC", "WARIO", "IZKEWL", "PASTE", "RATIO", "GRUNKLE", "LIBERALS", "AUSTRALIA" };
    private static readonly string[] _fakeCalls = new string[] { "WINE", "ARROWS", "SPEARS", "ZOMBIES", "BOLDER", "STREAM", "TPIRWIRE", "GHAST", "SPIKE", "SNAKE", "SCORPIONS", "SPIRITS", "PEARS", "HOLE", "PYLON", "SEAN", "SHAWN", "THOMAS", "THAMES", "NIORPOCS", "BEET", "KPWRQANTC", "ISCOOL", "PASTA", "GRUNKIE", "LIBERAL", "AUSTRIA", "GOOSE", "HOOP", "LUIGI", "PAIR", "CIDER", "BEETLE", "FUNGUS", "POLE", "REPUBLICANS", "VAMPIRE", "ROPE", "PEWDIEPIE", "CAKE", "VINE", "PIT", "WALL", "ARROW", "SPIDER", "SPEAR", "ZOMBIE", "BOULDER", "RIVER", "LOOT", "TRIPWIRE", "FIRE", "GHOST", "MOLE", "DUCK", "CROSSBOW", "SPIKES", "SCORPION", "SNAKES", "SPIRIT", "PEAR", "VOLE", "HALL", "TOAST", "HOOT", "CHUNGUS", "PYLONS", "CHILD SUPPORT", "BRAMBLEGAMING", "SHAUN", "SCOPRION", "TECHNO", "TAHMIS", "MARIO", "SCORPOIN", "SCOPROIN", "NIOPROCS", "BEAT", "KPWRAQNTC", "WARIO", "IZKEWL", "PASTE", "RATIO", "GRUNKLE", "LIBERALS", "AUSTRALIA" };
    private List<string> _chosenResponses = new List<string>();
    private List<string> _chosenCalls = new List<string>();

    private Coroutine _initialCycle;

    private string _currentInput = "";
    private bool _isSelected;
    private bool _canType;
    private string _expectedInput = "";
    private bool _canActivate;
    private int _deactivCount;
    private bool _isStrike;
    private bool _timerExpired;
    private bool _letTimerExpire;
    private int _preActivations = 0;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _chosenResponses = _responses.Shuffle().Take(4).ToList();
        _chosenCalls = _calls.Shuffle().Take(4).ToList();
        for (int i = 0; i < 4; i++)
            Debug.LogFormat("[The Temple Run #{0}] Initial response/call: YOU MUST [{1}] THE [{2}].", _moduleId, _chosenResponses[i], _chosenCalls[i]);
        var ModSelectable = GetComponent<KMSelectable>();
        ModSelectable.OnFocus += delegate () { _isSelected = true; };
        ModSelectable.OnDefocus += delegate () { _isSelected = false; };

        _initialCycle = StartCoroutine(InitialCycle());
        Needy.OnNeedyActivation += Activate;
        Needy.OnNeedyDeactivation += Deactivate;
        Needy.OnTimerExpired += OnTimerExpre;
    }

    private void Activate()
    {
        if (_preActivations < 7)
        {
            Debug.LogFormat("[The Temple Run #{0}] Activation bypassed to give time for reads.", _moduleId);
            _preActivations++;
            Deactivate();
            return;
        }
        _canActivate = true;
        if (_initialCycle != null)
            StopCoroutine(_initialCycle);
        _currentInput = "";
        _canType = true;
        _timerExpired = false;
        _isStrike = false;
        TopText.text = "YOU MUST []";
        if (Rnd.Range(0, 5) < 4)
        {
            var ix = Rnd.Range(0, _chosenCalls.Count);
            var chosen = _chosenCalls[ix];
            BottomText.text = "THE [" + chosen + "]";
            _expectedInput = _chosenResponses[ix];
            _letTimerExpire = false;
            Debug.LogFormat("[The Temple Run #{0}] Activated with prompt: YOU MUST [RESPONSE] THE [{1}].", _moduleId, chosen);
            Debug.LogFormat("[The Temple Run #{0}] This call was shown at the start! Expecting response: [{1}]", _moduleId, _expectedInput);
        }
        else
        {
            newRand:
            var ix = Rnd.Range(0, _fakeCalls.Length);
            var chosen = _fakeCalls[ix];
            if (_chosenCalls.Contains(chosen))
                goto newRand;
            BottomText.text = "THE [" + chosen + "]";
            _expectedInput = "";
            _letTimerExpire = true;
            Debug.LogFormat("[The Temple Run #{0}] Activated with prompt: YOU MUST [RESPONSE] THE [{1}].", _moduleId, chosen);
            Debug.LogFormat("[The Temple Run #{0}] This call was not shown at the start. Let the timer run out.", _moduleId);
        }
    }

    private void Deactivate()
    {
        if (!_canActivate)
        {
            Needy.HandlePass();
            return;
        }
        _canType = false;
        Needy.HandlePass();
        if (!_isStrike)
        {
            TopText.text = "";
            BottomText.text = "";
            _deactivCount++;
            if (_deactivCount == 1 || _deactivCount == 3 || (_deactivCount % 4 == 2 && _deactivCount != 2))
                GenNewCall();
        }
    }

    private void GenNewCall()
    {
        var rnd1 = _responses[Rnd.Range(0, _responses.Length)];
        var rnd2 = _calls[Rnd.Range(0, _calls.Length)];
        if (_chosenCalls.Contains(rnd2))
        {
            int ix = Array.IndexOf(_chosenCalls.ToArray(), rnd2);
            _chosenCalls[ix] = rnd2;
            _chosenResponses[ix] = rnd1;
        }
        else
        {
            _chosenResponses.Add(rnd1);
            _chosenCalls.Add(rnd2);
        }
        TopText.text = "YOU MUST [" + rnd1 + "]";
        BottomText.text = "THE [" + rnd2 + "]";
        Debug.LogFormat("[The Temple Run #{0}] Added new response/call: YOU MUST [{1}] THE [{2}].", _moduleId, rnd1, rnd2);
    }

    private void OnTimerExpre()
    {
        _currentInput = "";
        _timerExpired = true;
        SubmitAnswer();
    }

    private IEnumerator InitialCycle()
    {
        int index = 0;
        while (true)
        {
            TopText.text = "YOU MUST [" + _chosenResponses[index] + "]";
            BottomText.text = "THE [" + _chosenCalls[index] + "]";
            yield return new WaitForSeconds(2f);
            index = (index + 1) % 4;
        }
    }

    private void OnGUI()
    {
        if (!_isSelected)
            return;
        Event e = Event.current;
        if (e.type != EventType.KeyDown)
            return;
        ProcessKey(e.keyCode);
    }

    private void ProcessKey(KeyCode key)
    {
        if (!_canType)
            return;
        if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
        {
            _timerExpired = false;
            SubmitAnswer();
            return;
        }
        if (key == KeyCode.Backspace && _currentInput.Length > 0)
        {
            _currentInput = _currentInput.Remove(_currentInput.Length - 1);
            TopText.text = "YOU MUST [" + _currentInput + "]";
        }
        if (key == KeyCode.Space)
        {
            if (_currentInput.Length == 0)
                return;
            _currentInput += " ";
            TopText.text = "YOU MUST [" + _currentInput + "]";
        }
        if (key >= KeyCode.A && key <= KeyCode.Z)
        {
            string add = key.ToString().ToUpperInvariant();
            _currentInput += add;
            TopText.text = "YOU MUST [" + _currentInput + "]";
        }
        Audio.PlaySoundAtTransform("Press", transform);
    }

    private void SubmitAnswer()
    {
        _currentInput = _currentInput.ToUpperInvariant();
        _currentInput = Regex.Replace(_currentInput, @"\s+", " ");
        if (_currentInput != "" && _currentInput.Substring(_currentInput.Length - 1, 1) == " ")
            _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
        if (_timerExpired)
        {
            if (!_letTimerExpire)
            {
                Debug.LogFormat("[The Temple Run #{0}] Incorrectly let the timer run out, when you should've submitted [{1}]. Strike.", _moduleId, _expectedInput);
                _isStrike = true;
                Needy.HandleStrike();
                Deactivate();
                TopText.text = "YOU MUST [" + _expectedInput + "]";
            }
            else
            {
                Audio.PlaySoundAtTransform("Correct", transform);
                Debug.LogFormat("[The Temple Run #{0}] Correctly let the timer run out. Needy disarmed.", _moduleId);
                Deactivate();
            }
        }
        else if (_currentInput == _expectedInput)
        {
            Audio.PlaySoundAtTransform("Correct", transform);
            Debug.LogFormat("[The Temple Run #{0}] Correctly submitted [{1}]. Needy disarmed.", _moduleId, _currentInput);
            Needy.HandlePass();
            Deactivate();
        }
        else if (_letTimerExpire)
        {
            Debug.LogFormat("[The Temple Run #{0}] Incorrectly submitted [{1}], when you should've let the timer run out. Strike.", _moduleId, _currentInput);
            _isStrike = true;
            Needy.HandleStrike();
            Deactivate();
            TopText.text = "YOU MUST []";
        }
        else
        {
            Debug.LogFormat("[The Temple Run #{0}] Incorrectly submitted [{1}], when [{2}] was expected. Needy disarmed.", _moduleId, _currentInput, _expectedInput);
            _isStrike = true;
            Needy.HandleStrike();
            Deactivate();
            TopText.text = "YOU MUST [" + _expectedInput + "]";
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} submit <prompt> [Submits the current prompt as your response.]";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        var pieces = command.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
        if (pieces.Length < 2)
            yield break;
        if (pieces[0] != "submit")
            yield break;
        yield return null;
        if (!_canType)
        {
            yield return "sendtochaterror You can't submit an answer while the module isn't activated!";
            yield break;
        }
        var p = new List<string>();
        for (int i = 1; i < pieces.Length; i++)
            p.Add(pieces[i]);
        var submission = p.Join(" ");
        for (int i = 1; i < submission.Length + 1; i++)
        {
            _currentInput = submission.Substring(0, i);
            Audio.PlaySoundAtTransform("Press", transform);
            TopText.text = "YOU MUST [" + _currentInput + "]";
            yield return new WaitForSeconds(0.05f);
        }
        SubmitAnswer();
    }
}
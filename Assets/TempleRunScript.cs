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

    private static readonly string[] _calls = new string[] { "SWING ON", "JUMP OVER", "CLIMB", "DUCK UNDER", "STOMP ON", "DODGE", "CHOP", "RUN FROM", "SWIM THROUGH", "GRAB", "LEAP OVER", "BLOW ON", "BOO", "TRAP", "QUACK AT", "SWAY AROUND", "TREAD ON", "CRUSH", "HISS AT", "AHH AT", "SAY FRUIT AT", "UNDERMINE", "PASS THROUGH", "GO MMM AT", "OWL?", "BIG", "CONSTRUCT", "PAY", "SUBSCRIBE TO", "SWAP WITH", "CRSUH", "SPONGE", "HUG", "SUPER", "CRSHU", "RCSHU", "HSURC", "JUJU ON", "VQCPEDHCU", "WAH", "QUOTE", "COPY", "COPE WITH", "BAN", "OWN", "NUKE" };
    private static readonly string[] _responses = new string[] { "WINE", "ARROWS", "SPEARS", "ZOMBIES", "BOLDER", "STREAM", "TPIRWIRE", "GHAST", "SPIKE", "SNAKE", "SCORPIONS", "SPIRITS", "PEARS", "HOLE", "PYLON", "SEAN", "SHAWN", "THOMAS", "THAMES", "NIORPOCS", "BEET", "KPWRQANTC", "ISCOOL", "PASTA", "GRUNKIE", "LIBERAL", "AUSTRIA", "GOOSE", "HOOP", "LUIGI", "PAIR", "CIDER", "BEETLE", "FUNGUS", "POLE", "REPUBLICANS", "VAMPIRE", "ROPE", "PEWDIEPIE", "CAKE", "VINE", "PIT", "WALL", "ARROW", "SPIDER", "SPEAR", "ZOMBIE", "BOULDER", "RIVER", "LOOT", "TRIPWIRE", "FIRE", "GHOST", "MOLE", "DUCK", "CROSSBOW", "SPIKES", "SCORPION", "SNAKES", "SPIRIT", "PEAR", "VOLE", "HALL", "TOAST", "HOOT", "CHUNGUS", "PYLONS", "CHILD SUPPORT", "BRAMBLEGAMING", "SHAUN", "SCOPRION", "TECHNO", "TAHMIS", "MARIO", "SCORPOIN", "SCOPROIN", "NIOPROCS", "BEAT", "KPWRAQNTC", "WARIO", "IZKEWL", "PASTE", "RATIO", "GRUNKLE", "LIBERALS", "AUSTRALIA" };
    private const int _constCount = 4;
    private string[] _chosenCalls = new string[_constCount];
    private string[] _chosenResponses = new string[_constCount];

    private Coroutine _initialCycle;

    private string _currentInput = "";
    private bool _isSelected;
    private bool _canType;
    private string _expectedInput = "";
    private bool _canActivate;

    private void Start()
    {
        _moduleId = _moduleIdCounter++;
        _chosenCalls = _calls.Shuffle().Take(4).ToArray();
        _chosenResponses = _responses.Shuffle().Take(4).ToArray();
        for (int i = 0; i < 4; i++)
            Debug.LogFormat("[Temple Run #{0}] You must {1} the {2}.", _moduleId, _chosenCalls[i], _chosenResponses[i]);
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
        if (!_canActivate)
        {
            Deactivate();
            return;
        }
        if (_initialCycle != null)
            StopCoroutine(_initialCycle);
        _currentInput = "";
        _canType = true;
        BottomText.text = "";
        if (Rnd.Range(0, 20) < 13)
        {
            var ix = Rnd.Range(0, _constCount);
            var chosen = _chosenCalls[ix];
            TopText.text = "YOU MUST " + chosen + " THE";
            _expectedInput = _chosenResponses[ix];
            Debug.LogFormat("[Temple Run #{0}] Activated with prompt: YOU MUST [{1}] THE [RESPONSE].", _moduleId, chosen);
            Debug.LogFormat("[Temple Run #{0}] This call was shown at the start! Expecting response: [{1}]", _moduleId, _expectedInput);
        }
        else
        {
            newRand:
            var ix = Rnd.Range(0, _calls.Length);
            var chosen = _calls[ix];
            if (_chosenCalls.Contains(chosen))
                goto newRand;
            TopText.text = "YOU MUST " + _calls[ix] + " THE";
            _expectedInput = "";
            Debug.LogFormat("[Temple Run #{0}] Activated with prompt: YOU MUST [{1}] THE [RESPONSE].", _moduleId, chosen);
            Debug.LogFormat("[Temple Run #{0}] This call was not shown at the start. Let the timer run out.", _moduleId);
        }
    }

    private void Deactivate()
    {
        if (!_canActivate)
        {
            Needy.HandlePass();
            return;
        }
        TopText.text = "";
        BottomText.text = "";
        _canType = false;
        Needy.HandlePass();
    }

    private void OnTimerExpre()
    {
        _currentInput = "";
        SubmitAnswer();
    }

    private IEnumerator InitialCycle()
    {
        int activIx = 0;
        int index = 0;
        while (true)
        {
            TopText.text = "YOU MUST " + _chosenCalls[index] + " THE";
            BottomText.text = _chosenResponses[index];
            yield return new WaitForSeconds(1.5f);
            index = (index + 1) % _constCount;
            activIx++;
            if (activIx > 30)
                _canActivate = true;
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

    private bool ProcessKey(KeyCode key)
    {
        if (!_canType)
            return false;
        if (key == KeyCode.Return || key == KeyCode.KeypadEnter)
        {
            SubmitAnswer();
            return true;
        }
        if (key == KeyCode.Backspace && _currentInput.Length > 0)
        {
            _currentInput = _currentInput.Remove(_currentInput.Length - 1);
            BottomText.text = _currentInput;
            return true;
        }
        if (key == KeyCode.Space)
        {
            if (_currentInput.Length == 0)
                return true;
            _currentInput += " ";
            BottomText.text = _currentInput;
            return true;
        }
        if (key >= KeyCode.A && key <= KeyCode.Z)
        {
            string add = key.ToString().ToUpperInvariant();
            _currentInput += add;
            BottomText.text = _currentInput;
            return true;
        }
        return false;
    }


    private void SubmitAnswer()
    {
        _currentInput = _currentInput.ToUpperInvariant();
        _currentInput = Regex.Replace(_currentInput, @"\s+", " ");
        if (_currentInput != "" && _currentInput.Substring(_currentInput.Length - 1, 1) == " ")
            _currentInput = _currentInput.Substring(0, _currentInput.Length - 1);
        if (_currentInput == _expectedInput)
        {
            Debug.LogFormat("[Temple Run #{0}] Succesfully submitted {1}. Needy disarmed.", _moduleId, _currentInput == "" ? "no input" : _currentInput);
            Needy.HandlePass();
             Deactivate();
        }
        else
        {
            Debug.LogFormat("[Temple Run #{0}] Incorrectly submitted {1}, when {2} was expected. Needy disarmed.", _moduleId, _currentInput == "" ? "no input" : _currentInput, _expectedInput == "" ? "no input" : _expectedInput);
            Needy.HandleStrike();
            Deactivate();
        }
    }

#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} submit <prompt> [Submits the current prompt as your answer.]";
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
            BottomText.text = _currentInput;
            yield return new WaitForSeconds(0.02f);
        }
        SubmitAnswer();
    }
}

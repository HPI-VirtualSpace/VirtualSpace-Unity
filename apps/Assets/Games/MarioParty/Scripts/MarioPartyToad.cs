using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VirtualSpace;
using VirtualSpace.Shared;
using Random = UnityEngine.Random;

public class MarioPartyToad : MonoBehaviour
{
    public float FlagTime = 1f;
    public Vector3 RotMin, RotMax;
    public Transform Arm;
    public Renderer FlagRenderer;
    public GameObject[] Mushrooms;
    public Color[] Colors;
    public float UpDownTime, UpHeight, DownHeight;
    public VirtualSpaceHandler VirtualSpaceHandler;
    public float ValidDistance = 0.8f;
    public float ValidPauseForFlagRaise = 5f;
    public float ValidPauseNoFlagRaise = 2f;
    public float ValidTransitionTime = 2f;
    public GameObject EffectSelect;
    public AudioClip Hi, Regret, Success1, Success2;
    public AudioSource Source;
    public GameObject Instruction;
    public Text InstructionText;

    private float _flagTime;
    private List<int> _colorsShow;
    private List<AnimData> _data;
    private float _current;
    private bool _switchFlag;
    private List<int> _usedMushrooms;

    public class AnimData
    {
       public int index;
        public bool anim;
        public float time;
    }

    void OnEnable()
    {
        VirtualSpaceHandler.OnReceivedNewEvents += OnReceivedNewEvents;
    }

    void OnDisable()
    {
        VirtualSpaceHandler.OnReceivedNewEvents -= OnReceivedNewEvents;
    }

    private void OnReceivedNewEvents()
    {
        //if (_data.Count == 0)
        //    return;
        //if (_data[0].single > 1000 || _data[0].all > 1000)
        //{
        //    _data.Clear();
        //}
    }

    void Awake()
    {
        _data = new List<AnimData>();
        _usedMushrooms = new List<int>();
    }

    void Update ()
	{
        if (Time.time < 4f)
            return;
        Instruction.SetActive(false);

        if (_switchFlag)
	    {
	        _current += Time.deltaTime;
	        var factor = Mathf.Clamp01(_current / _flagTime);
	        var rotFactor = 1f - 2f * Mathf.Abs(factor - 0.5f);
	        Arm.localRotation = Quaternion.Euler(Vector3.Lerp(RotMin, RotMax, rotFactor));
            Instruction.SetActive(true);
            InstructionText.text = "Pay attention!";
            if (_current >= _flagTime)
	        {
	            if (_colorsShow.Any())
                {
                    var index = _colorsShow.First();
                    var color = Colors[index];
	                color.a = color.a / 2f;
                    FlagRenderer.material.color = Colors[index];
                    FlagRenderer.material.SetColor("_EmissionColor", color);
	                _current = 0f;
                    var effectSelect = Instantiate(EffectSelect);
                    var pos = Mushrooms[index].transform.position;
                    //pos.y = UpHeight;
                    effectSelect.transform.position = pos;
                    effectSelect.GetComponent<ParticleSystem>().startColor = color;
                    effectSelect.SetActive(true);
                    _colorsShow.RemoveAt(0);
                }
	            else
	                _switchFlag = false;
	        }
        }

        transform.LookAt(Camera.main.transform);

	    //for(var i = 0; i < Mushrooms.Length; i++)
	    //var maybeReplan = false;
	    if (_data.Any())
	    {
	        if (_data[0].time > 0f)
	        {
	            _data[0].time -= Time.deltaTime;

                // var factor = Mathf.Clamp01((DownTime - _all[0]) / DownTime);
                if (_data[0].anim)
                {
                    var diff = (Time.deltaTime / UpDownTime) * (UpHeight - DownHeight);
                    for (var i = 0; i < Mushrooms.Length; i++)
                    {
                        //if (i == _data[0].index)
                        //    continue;
                        var pos = Mushrooms[i].transform.localPosition;
                        //pos.y = (1f - factor) * UpHeight + factor * DownHeight;

                        pos.y += _data[0].time > UpDownTime ? diff : (_data[0].index == i ? 0 : -diff);
                        pos.y = Mathf.Clamp(pos.y, DownHeight, UpHeight);
                        Mushrooms[i].transform.localPosition = pos;
                    }
                }

                if (_data[0].time < 0f)
                {
                    if(_data[0].index >= 0)
                    {
                        RaycastHit info;
                        var clip = Regret;
                        if(Physics.Raycast(new Ray(Camera.main.transform.position, Vector3.down), out info, 4f))
                        {
                            if(info.collider.gameObject == Mushrooms[_data[0].index])
                            {
                                clip = Random.Range(0, 2) == 0 ? Success1 : Success2;
                            }
                        }
                        Source.clip = clip;
                        Source.Play();
                    }

                    if (_data[0].index >= 0)
                        _usedMushrooms.Add(_data[0].index);

                    _data.RemoveAt(0);

                    

                    //maybeReplan = !_data.Any();
                }

            }
            //   else if (_data[0].single > 0f)
            //{
            //    _data[0].single -= Time.deltaTime;

            //    //var factor = Mathf.Clamp01((UpTime - _single[0]) / UpTime);
            //    for (var i = 0; i < Mushrooms.Length; i++)
            //    {
            //        if (i == _data[0].index)
            //            continue;
            //        var pos = Mushrooms[i].transform.localPosition;
            //        //pos.y = factor * UpHeight + (1f - factor) * DownHeight;
            //        pos.y -= (Time.deltaTime / DownTime) * (UpHeight - DownHeight);
            //        pos.y = Mathf.Max(DownHeight, pos.y);
            //        Mushrooms[i].transform.localPosition = pos;
            //    }

            //    if (_data[0].single < 0f)
            //    {
            //        _data.RemoveAt(0);

            //        maybeReplan = true;
            //    }
            //}
        }else
	    {
            //check if pause long enough, 
	        List<List<Vector3>> areas;
	        List<float> start;
	        List<float> end;
	        var longPause = false;
	        if (GetNextTransitions(out areas, out start, out end))
	        {
	            for (var i = 0; i < start.Count; i++)
	            {
	                var pause = i == 0 ? start[i] : start[i] - end[i - 1];
	                if (pause >= ValidPauseForFlagRaise)
	                {
	                    longPause = true;
	                    break;
	                }
	            }
	        }

            //if yes, replan
	        if (longPause)
	        {
	             _data.Clear();
	        }
        }
        if (_data.Count == 0)
        {
            List<List<Vector3>> areas;
            List<float> start;
            List<float> end;
            if (GetNextTransitions(out areas, out start, out end))
            {
                var count = start.Count;

                var indices = areas.Select(MapAreaToRandomMushroom).ToList();
                //Debug.Log(indices.Count + " !");
                var lastEnd = 0f;
                //for mushroom movement
                for (var i = 0; i < count; i++)
                {
                    //Debug.Log(Mushrooms[indices[i]].name + " / " + start[i] + " / " + end[i]);
                    //var tmpTime = i > 0 ? start[i] : 0f;
                    var emptyData = new AnimData
                    {
                        anim = false,
                        time = start[i] - lastEnd,//Mathf.Max(end[i] - lastEnd - ValidTransitionTime, UpTime),
                        index = -1
                    };
                    _data.Add(emptyData);
                    var newData = new AnimData
                    {
                        //all = Mathf.Max(end[i] - tmpTime - DownTime, UpTime),
                        //single = DownTime,
                        anim = true,
                        time = end[i]-start[i],//Mathf.Max(end[i] - lastEnd - ValidTransitionTime, UpTime),
                        index = indices[i]
                    };
                    lastEnd = end[i];
                    //if(i > 0)
                    //    _data[i-1].single = Mathf.Max(start[i] - end[i - 1], DownTime);
                    _data.Add(newData);
                    
                }

                //raise flag
                _flagTime = FlagTime / count;
                _colorsShow = new List<int>(indices);
                _current = 100f;
                _switchFlag = _colorsShow.Any();
                Source.clip = Hi;
                Source.Play();
            }
        }
        //else
        //{
        //    for (var i = 0; i < _data.Count; i++)
        //    {
        //        Debug.Log((_data[0].anim ? Mushrooms[_data[i].index].name : "no anim") + " " + _data[i].time);
        //    }
        //}

        //check if there is a new state in backend
        if (VirtualSpaceHandler.GetNewState())
	    {
	        EvaluateStates(VirtualSpaceHandler.State);
	    }
    }

    private List<int> GetMushroomHistoryAndCurrent()
    {
        var history = new List<int>(_usedMushrooms);
        var current = _data.Where(d => d.index >= 0).Select(d => d.index);
        history.AddRange(current);
        return history;
    }

    private void EvaluateStates(StateInfo info)
    {
        if (!VirtualSpaceCore.Instance.IsRegistered() || info == null)
            return;
        
        TransitionVoting voting = new TransitionVoting { StateId = info.StateId };

        var shouldShowFlagNext = true;
        for (var i = 0; i < info.PastTransitions.Count; i++)
        {
            var plannedTransition = info.PastTransitions[i];
            var waitUntilStart = i == 0 ? plannedTransition.FromSeconds : plannedTransition.FromSeconds - info.PastTransitions[i - 1].ToSeconds;
            if (shouldShowFlagNext && waitUntilStart >= ValidPauseForFlagRaise && i < 3)
                shouldShowFlagNext = false;
        }

        //var areaLast = info.PastTransitions.Last().ToArea;

        //var overlaps = new List<float>();
        var history = GetMushroomHistoryAndCurrent();
        var historyCount = new List<float>();
        for (var i = 0; i < info.PossibleTransitions.Count; i++)
        {
            var areaMaybe = info.TransitionEndAreas[i];
            var unityAreaMaybe = VirtualSpaceHandler._TranslateIntoUnityCoordinates(areaMaybe);
            var mushroomMaybe = MapAreaToMushroom(unityAreaMaybe);
            var count = history.Where(h => h == mushroomMaybe).Count();
            historyCount.Add(count);
            //Debug.Log("count " + Mushrooms[mushroomMaybe].name + " " + count);
            //var over = (float) ClipperUtility.GetArea(ClipperUtility.Intersection(areaLast, areaMaybe));
            //overlaps.Add(over);
        }

        var valuation = new Dictionary<int, float>();
        for (var i = 0; i < info.PossibleTransitions.Count; i++)
        {
            valuation.Add(i, 0);
        }

        //states low used mushrooms are more important
        var countMax = historyCount.Max();
        var countMin = historyCount.Min();
        if (countMax - countMin > 0)
        {
            for (var i = 0; i < info.PossibleTransitions.Count; i++)
            {
                var val = 1f - (float)((historyCount[i] - countMin) / (countMax - countMin));
                valuation[i] += val;
            }
        }

        ////states with low overlap to current area are important
        //var overlapMax = overlaps.Max();
        //var overlapMin = overlaps.Min();
        //if (overlapMax - overlapMin > 0)
        //{
        //    for (var i = 0; i < info.PossibleTransitions.Count; i++)
        //    {
        //        var val = 1f - (float)((overlaps[i] - overlapMin) / (overlapMax - overlapMin));
        //        valuation[i] += val;
        //    }
        //}

        //normalize
        var valueMax = valuation.Values.Max();
        var weights = valuation.Select(v => 0).ToArray();
        var maxWeight = 100;
        for (var i = 0; i < valuation.Count; i++)
        {
            weights[i] = valueMax > 0f ? (int)(maxWeight * valuation[i] / valueMax) : 0;
        }

        //valuate
        for (var i = 0; i < info.PossibleTransitions.Count; i++)
        {
            var transition = info.PossibleTransitions[i];
            var vote = new TransitionVote
            {
                Transition = transition,
                Value = weights[i]
            };
            
                vote.PlanningTimestampMs = new List<double> { (shouldShowFlagNext ? ValidPauseForFlagRaise : ValidPauseNoFlagRaise) * 1000 };
                vote.ExecutionLengthMs = new List<double> { ValidTransitionTime * 1000};
            
            voting.Votes.Add(vote);

            //Debug.Log("vote " + vote.Value + " " + vote.PlanningTimestampMs.First() + " " + vote.ExecutionLengthMs.First() + " " + vote.TransitionName);
        }

        VirtualSpaceCore.Instance.SendReliable(voting);
    }

    private bool GetNextTransitions(out List<List<Vector3>> areas, out List<float> start, out List<float> end)
    {
        start = new List<float>();
        end = new List<float>();
        areas = new List<List<Vector3>>();
        if (!VirtualSpaceCore.Instance.IsRegistered()) return false;
        
        var incoming = VirtualSpaceHandler.IncomingTransitions;
        while (incoming.Any())
        {
            var next = incoming.First();
            incoming.Remove(next);
            if(next.TransitionContext != TransitionContext.Animation)
                continue;
            
            var area = next.Frames.Last().Area.Area;
            var areaAsVectors = VirtualSpaceHandler._TranslateIntoUnityCoordinates(area);
            areas.Add(areaAsVectors);
            start.Add(next.SecondsToStart);
            end.Add(next.SecondsToEnd);
        }
        return true;
    }

    public int MapAreaToRandomMushroom(List<Vector3> area)
    {
        var distances = Mushrooms.Select(m => VirtualSpaceHandler.DistanceFromPoly(m.transform.position, area, false))
            .ToList();
        var history = GetMushroomHistoryAndCurrent();
        var last = history.Any() ? history.Last() : -1;
        var minIndex = distances.IndexOf(distances.Min());
        var validIndices = new List<int>();
        for (var i = 0; i < distances.Count; i++)
        {
            if (distances[i] < ValidDistance && i != last)
            {
                validIndices.Add(i);
            }
        }
        return validIndices.Any() ? validIndices[Random.Range(0, validIndices.Count)] : minIndex;
    }

    public int MapAreaToMushroom(List<Vector3> area)
    {
        var distances = Mushrooms.Select(m => VirtualSpaceHandler.DistanceFromPoly(m.transform.position, area, false))
            .ToList();
        var minIndex = distances.IndexOf(distances.Min());
        return minIndex;
    }

    //public void RaiseFlags(List<int> indices)
    //{
        

        
    //    //foreach (var tw in _timeWait)
    //    //    tw.Clear();
    //    //foreach (var tu in _timeUp)
    //    //    tu.Clear();
    //    //for (var i = 0; i < indices.Count; i++)
    //    //{
    //    //    Debug.Log(Mushrooms[indices[i]].gameObject.name);
    //    //    var addTime = 0f;
    //    //    for (var j = 0; j < i; j++)
    //    //    {
    //    //        addTime += upTimes[j] + waitTimes[j];
    //    //    }
    //    //    _timeWait[indices[i]].Add(waitTimes[i]);// + addTime - TransitionTime);
    //    //    Debug.Log("wait "+ waitTimes[i] + " " + addTime + " " + TransitionTime + " = " + (waitTimes[i] + addTime - TransitionTime));
    //    //    _timeUp[indices[i]].Add(upTimes[i]);
    //    //    Debug.Log("up " + upTimes[i]);
    //    //}
    //}
}

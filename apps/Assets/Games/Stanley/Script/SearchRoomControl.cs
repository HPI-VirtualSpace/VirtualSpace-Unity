using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchRoomControl : RoomControl
{
    [InspectorButton("UserFoundCrystal")]
    public bool Found;

    // template
    public MagicCrystal Crystal;
    public ParticleSystem CrystalDisappearsTemplate;

    // trigger references
    public DetectIfEntered PlayerEnteredTrigger;
    public DetectIfEntered CrystalEnteredTrigger;

    // state
    public int NumCrystalToFind = 3;

    [SerializeField]
    private int _numCrystalToFindLeft = 0;
    private MagicCrystal _hiddenCrystal = null;
    public Transform Center;

    // const
    private const string CrystalObjectName = "CrystalToFind";
    private const float CrystalPuffLiveTime = 3f;

    // container 
    public Transform CrystalHideLocationsParent;
    private Transform _tempObjectContainer;

    // action when too far away from user, create new one

    void Awake()
    {
        _tempObjectContainer = new GameObject().transform;
    }
    
    public override void ResetRoom()
    {
        // reset state
        _numCrystalToFindLeft = NumCrystalToFind;
    }

    public override void InitializeRoom()
    {
        
    }

    private void OnEnable()
    {
        PlayerEnteredTrigger.OnTriggerEnterAction += UserEntered;
        PlayerEnteredTrigger.gameObject.SetActive(true);
        CrystalEnteredTrigger.OnTriggerEnterAction += UserFoundCrystal;
        CrystalEnteredTrigger.ObjectName = CrystalObjectName;

        ResetRoom();
    }

    private void OnDisable()
    {
        PlayerEnteredTrigger.OnTriggerEnterAction -= UserEntered;
    }

    protected override void UserEntered()
    {
        base.UserEntered();

        PlayerEnteredTrigger.gameObject.SetActive(false);

        HideSingle();
    }

    private float CrystalRespawnDistance = 3f;
    void Update()
    {
        if (_hiddenCrystal != null &&
            Vector3.Distance(_hiddenCrystal.transform.position, Center.position) >= CrystalRespawnDistance)
        {
            DeleteActiveCrystal();
            HideSingle();
        }
    }

    private void UserFoundCrystal()
    {
        if (_numCrystalToFindLeft < 0)
        {
            Debug.LogWarning("Found more crystals than should have been spawned");
            Debug.LogWarning("Doing nothing");
            return;
        }

        _numCrystalToFindLeft--;

        DeleteActiveCrystal();

        
        if (_numCrystalToFindLeft > 0) {
            HideSingle();
        }

        if (_numCrystalToFindLeft == 1)
        {
            UserAboutToFinish();
        }

        if (_numCrystalToFindLeft == 0)
        {
            UserFinished();
        }
    }
    
    private Vector3 GetRandomHideWorldLocation()
    {
        var childCount = CrystalHideLocationsParent.childCount;
        var childIndex = Random.Range(0, childCount);
        var hideLocation = CrystalHideLocationsParent.GetChild(childIndex);
        return hideLocation.position;
    }

    public void DeleteActiveCrystal()
    {
        if (_hiddenCrystal == null)
        {
            Debug.LogWarning("Trying to destroy active crystal but there is no active object.");
            return;
        }

        _hiddenCrystal.PlayPuff();
        Destroy(_hiddenCrystal.gameObject);
        var crystalPuff = Instantiate(CrystalDisappearsTemplate, _hiddenCrystal.transform.position,
            Quaternion.identity);
        Destroy(crystalPuff, CrystalPuffLiveTime);

        _hiddenCrystal = null;
    }

    public void HideSingle()
    {
        var hidePosition = GetRandomHideWorldLocation();
        _hiddenCrystal = Instantiate(Crystal);
        _hiddenCrystal.PlayPuff();
        _hiddenCrystal.gameObject.name = CrystalObjectName;
        _hiddenCrystal.transform.parent = _tempObjectContainer;
        _hiddenCrystal.transform.position = hidePosition;
    }

    
}

using EasyButtons;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;
using DG.Tweening;

public class Digimochi : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator[] animators;
    [SerializeField] private SpriteLibrary animationSpriteLibrary;

    [Header("State Thresholds")]
    [SerializeField, Min(1),Tooltip("Time in days to get dirty")] 
    private int daysToGetDirty = 1;

    [SerializeField,Min(1), Tooltip("Time in days to get hungry")] 
    private int daysToGetHungry = 1;

    [SerializeField, Min(1), Tooltip("Time in days to get sick")]
    private int daysToGetSick = 1;

    [Header("States")]
    [SerializeField] private List<DigimochiState> states;

    [SerializeField] private ParticleSystem bathParticles;
    [SerializeField] private ParticleSystem loveParticles;
    [SerializeField] private GameObject manzana;
    [SerializeField] private GameObject medicine;



    private DigimochiSO digimochiSO;
    private IDigimochiData digimochiData;
    private HapinessBarController hapinessBar;
    private bool isActive;

    public event Action ActionExecuted;
    public event Action ActionFinished;

    public enum DigimochiAnimations
    {
        Idle,
        Feed,
        Cure,
        Pet,
        Dance
    }

    public IDigimochiData DigimochiData => digimochiData;
    public DigimochiSO DigimochiSO => digimochiSO;

    private void Start()
    {
        Initialize();
        UpdateCurrentState();
    }

    public void ActiveDigimochi()
    {
        isActive = true;
        UpdateCurrentState();
    }

    public void DisableDigimochi()
    {
        isActive = false;
    }

    private void UpdateCurrentState()
    {
        SetState(DigimochiState.StateTypes.Hungry, IsHungry());
        SetState(DigimochiState.StateTypes.Sick, IsSick());
        SetState(DigimochiState.StateTypes.Dirty, IsDirty());

        UpdateHapinessBar();
    }

    private void UpdateHapinessBar()
    {
        //TODO: Definir mejor las condiciones que manejan la felicidad
        var statesActiveCount = states.Where(x => x.IsStateActive).Count();

        if( statesActiveCount == 0 )
        {
            hapinessBar.SetHapinessState
               (HapinessBarController.HapinessState.VeryHappy);

            return;
        }

        if (statesActiveCount == 1)
        {
            hapinessBar.SetHapinessState
               (HapinessBarController.HapinessState.Happy);

            return;
        }

        if (statesActiveCount == 2)
        {
            hapinessBar.SetHapinessState
               (HapinessBarController.HapinessState.Normal);

            return;
        }

        if (statesActiveCount >= 3)
        {
            hapinessBar.SetHapinessState
               (HapinessBarController.HapinessState.Sad);

            return;
        }
    }

    [Button]
    private void SetState(DigimochiState.StateTypes stateType, bool toggle)
    {
        // Debug.Log($"Set state: {stateType} {toggle}");

        var stateToEnable = states.Find(x => x.StateType == stateType);

        if (toggle)
        {
            stateToEnable.EnableState();
        }
        else
        {
            stateToEnable.DisableState();
        }
    }

    private bool IsHungry()
    {
        return IsDateExpired(digimochiData.GetLastMealTime(), daysToGetHungry);
    }

    private bool IsSick()
    {
        return IsDateExpired(digimochiData.GetLastMedicineTime(), daysToGetSick);
    }

    private bool IsDirty()
    {
        return IsDateExpired(digimochiData.GetLastBathTime(), daysToGetDirty);
    }

    [Button]
    public void SetAnimation(DigimochiAnimations animation)
    {
        for (int i = 0; i < animators.Length; i++)
        {
            animators[i].SetFloat("Blend", (int)animation);
        }
    }

    private bool IsDateExpired(DateTime date, int daysTreshold, bool debug = false)
    {
        DateTime todayDateTime = DateTime.Now;

        // Calculate the time difference
        TimeSpan timeSinceLastMeal = todayDateTime - date;

        // Check if the time difference is greater than daysToGetHungry
        bool isExpired = timeSinceLastMeal.TotalDays > daysTreshold;

        if (debug)
        {
            if (isExpired)
            {
                Debug.Log("Date expired");
            }
            else
            {
                Debug.Log("The date did not expire");
            }

            Debug.Log("Start date: " + date.ToString("dd/MM/yyyy HH:mm:ss"));
            Debug.Log("Today's date: " + todayDateTime.ToString("dd/MM/yyyy HH:mm:ss"));
            Debug.Log("Number of days between start and today: " + (int)timeSinceLastMeal.TotalDays);
        }

        return isExpired;
    }

    [Button]
    public void Bath() 
    {   
        StartCoroutine(BathCoroutine());
    }

    [Button]
    public void Feed()
    {
        StartCoroutine(FeedCoroutine());
    }

    [Button]
    public void Cure()
    {
        StartCoroutine(CureCoroutine());
    }

    [Button]
    public void Dance()
    {
        StartCoroutine(DanceCoroutine());
    }

    private IEnumerator BathCoroutine()
    {
        yield return new WaitForEndOfFrame();

        // Start bath animation...
        bathParticles.Play(true);
        SetAnimation(DigimochiAnimations.Pet);

        yield return new WaitForSeconds(5f);

        // Stop bath animation...
        bathParticles.Stop(true);
        SetAnimation(DigimochiAnimations.Idle);

        // Update States
        digimochiData.Bath();
        UpdateCurrentState();
    }

    private IEnumerator FeedCoroutine()
    {
        yield return new WaitForEndOfFrame();
        manzana.transform.position = new Vector2(0, 0.5f);
        manzana.SetActive(true);

        manzana.transform.DOMove(transform.position, 2f);
        yield return new WaitForSeconds(2f);
        
        manzana.SetActive(false);
        SetAnimation(DigimochiAnimations.Feed);
        yield return new WaitForSeconds(1f);

        SetAnimation(DigimochiAnimations.Pet);
        loveParticles.Play(true);
        yield return new WaitForSeconds(2f);

        loveParticles.Stop(false);
        SetAnimation(DigimochiAnimations.Idle);

        digimochiData.Feed();
        UpdateCurrentState();

        ActionFinished?.Invoke();
    }

    private IEnumerator CureCoroutine()
    {
        yield return new WaitForEndOfFrame();
        medicine.gameObject.SetActive(true);

        SetAnimation(DigimochiAnimations.Cure);
        yield return new WaitForSeconds(1f);

        SetAnimation(DigimochiAnimations.Idle);
        medicine.gameObject.SetActive(false);

        digimochiData.Cure();
        UpdateCurrentState();
        ActionFinished?.Invoke();
    }


    private IEnumerator DanceCoroutine()
    {
        yield return new WaitForEndOfFrame();

        SetAnimation(DigimochiAnimations.Dance);

        yield return new WaitForSeconds(2f);

        SetAnimation(DigimochiAnimations.Idle);


        UpdateCurrentState();
    }


    public void SetDigimochiSO(DigimochiSO type)
    {
        digimochiSO = type;
    }

    public void SetDigimochiData(IDigimochiData data)
    {
        digimochiData = data;
    }

    public void Initialize() 
    {
        gameObject.name = $"Digimochi_{digimochiSO.digimochiType}";
        animationSpriteLibrary.spriteLibraryAsset = digimochiSO.spriteLibraryIdleAnimation;
        SetAnimation(DigimochiAnimations.Idle);

        //TODO: Emprolijar esto
        hapinessBar = FindAnyObjectByType<HapinessBarController>();
    }

}
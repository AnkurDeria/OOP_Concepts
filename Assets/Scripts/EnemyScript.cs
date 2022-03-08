using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

#region Enemy behaviour type 1: enemy with no colors

public class EnemyBehaviourType1 : EnemyBaseClass, IDamageable
{
    /// <summary>
    /// Light attack having 100% chance of hit.
    /// </summary>
    /// <param name="_damage"></param>
    public void GotHit(int _damage = 1)
    {
        Health -= _damage;
        PostHitAnimation(true);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.Log("Enemy " + type + " number" + index + " got hit. Health left = " + Health);
#endif
    }

    /// <summary>
    /// Heavy attach which depends on _missChance to hit. If _missChance = 0.1 then there is a 90% chance to land a hit.
    /// </summary>
    /// <param name="_damage"></param>
    /// <param name="_missChance"></param>
    public void GotHit(int _damage = 1, float _missChance = 0f)
    {
        _damage = (Random.value > _missChance) ? _damage : 0;
        if (_damage > 0)
        {
            Health -= _damage;
            PostHitAnimation(false);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("Enemy " + type + " number" + index + " got hit. Health left = " + Health);
#endif
        }
    }
}

#endregion

////--------------------------------------------------------------------------------------------------------------------

#region Enemy behaviour type 2: enemy with colors which might have different responses to attacks and different death animations when compared to enemy type 1

public class EnemyBehaviourType2 : EnemyBaseClass, IDamageable, IChangeableColour
{
    private Material currentMat;

    private protected override void Awake()
    {
        currentMat = GetComponent<Renderer>().material;
        base.Awake();
    }

    private protected override void OnEnable()
    {
        base.OnEnable();

        // Set color at start of respawn and on instantiation
        ChangeColor();
    }

    /// <summary>
    /// Light attack having 100% chance of hit.
    /// </summary>
    /// <param name="_damage"></param>
    public void GotHit(int _damage = 1)
    {
        Health -= _damage;

        // Change color to show decrease in health
        ChangeColor();
        PostHitAnimation(true);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.Log("Enemy " + type + " number" + index + " got hit. Health left = " + Health);
#endif
    }



    /// <summary>
    /// Heavy attach which depends on _missChance to hit. If _missChance = 0.1 then there is a 90% chance to land a hit.
    /// </summary>
    /// <param name="_damage"></param>
    /// <param name="_missChance"></param>
    public void GotHit(int _damage = 1, float _missChance = 0f)
    {
        _damage = (Random.value > _missChance) ? _damage : 0;
        if (_damage > 0)
        {
            Health -= _damage;

            // Change color to show decrease in health
            ChangeColor();
            PostHitAnimation(false);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            Debug.Log("Enemy " + type + " number" + index + " got hit. Health left = " + Health);
#endif
        }
    }

    /// <summary>
    /// Change color according to current health. Color is chosen from a gradient.
    /// </summary>
    public void ChangeColor()
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.Log("Changing color");
#endif
        currentMat.color = GameManager.Instance.ColorGradient.Evaluate(Health / ((int)type * 3f));
    }


    /// <summary>
    /// Change color according to _life. Color is chosen from a gradient.
    /// </summary>
    /// <param name="_life"></param>
    public void ChangeColor(int _life)
    {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
        Debug.Log("Changing color");
#endif
        currentMat.color = GameManager.Instance.ColorGradient.Evaluate(_life);
    }

    /// <summary>
    /// Change color to the color for 0 life.
    /// </summary>
    private protected override void OnDeath()
    {
        ChangeColor(0);
        base.OnDeath();
    }
}

#endregion

////--------------------------------------------------------------------------------------------------------------------

#region Base class for enemy

public class EnemyBaseClass : MonoBehaviour
{
    // public variables
    public Collider collider;
    public int index;
    public Vector3 halfSize;



    // private variables
    [SerializeField] private int health;
    [SerializeField] private protected EnemyBodyType type = EnemyBodyType.CUBE;
    private Tween lightHitAnimation;
    private Tween heavyHitAnimation;




    public int Health
    {
        get
        {
            return health;
        }
        private protected set
        {
            health = value;
            if (health < 0)
            {
                OnDeath();
            }
        }
    }


    private protected virtual void Awake()
    {
        collider = GetComponent<Collider>();
        halfSize = GetComponent<Renderer>().GetComponent<Renderer>().bounds.size * 0.5f;
    }

    /// <summary>
    /// Make index as position in Parent Transform, initialize animations and turn off gameObject for now.
    /// </summary>
    private void Start()
    {
        index = transform.GetSiblingIndex();
        lightHitAnimation = transform.DOPunchScale(new Vector3(-0.3f, 0.6f, -0.3f), 0.3f, 1, 1f).SetEase(Ease.OutCirc).SetAutoKill(false).OnComplete(ResetScale);
        heavyHitAnimation = transform.DOPunchScale(new Vector3(0.3f, -0.6f, 0.3f), 0.3f, 1, 1f).SetEase(Ease.OutCirc).SetAutoKill(false).OnComplete(ResetScale);;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Show hit animation depending on type of hit
    /// </summary>
    /// <param name="_lightHit"></param>
    private protected void PostHitAnimation(bool _lightHit)
    {
        PauseHitAnimations();
        if (_lightHit)
        {
            lightHitAnimation.Restart();
        }
        else
        {
            heavyHitAnimation.Restart();
        }
    }

    public void SetEnemyType(EnemyBodyType _type)
    {
        type = _type;
    }

    /// <summary>
    /// Reset scale and health on enable i.e. respawn. Also enable collider.
    /// </summary>
    private protected virtual void OnEnable()
    {
        collider.enabled = true;
        transform.localScale = Vector3.one;
        health = (int)type * 3;
    }

    private void PauseHitAnimations()
    {
        // Pause hit animations on this transform
        lightHitAnimation.Pause();
        heavyHitAnimation.Pause();
    }

    private void ResetScale()
    {
        transform.localScale = Vector3.one;
    }

    /// <summary>
    /// Disable colliders on enemy death and start death animation.
    /// </summary>
    private protected virtual void OnDeath()
    {
        collider.enabled = false;
        PauseHitAnimations();
        transform.DOScale(Vector3.zero, GameManager.Instance.enemyType2KillAnimationTimeInSeconds).SetEase(Ease.InBounce).OnComplete(() => gameObject.SetActive(false));
    }
}

#endregion

////--------------------------------------------------------------------------------------------------------------------

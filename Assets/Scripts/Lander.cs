using System;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class Lander : MonoBehaviour
{

    private const float GRAVITY_NORMAL = 0.7f;

    public static Lander Instance { get; private set;  }

    public event EventHandler OnUpForce;
    public event EventHandler OnRightForce;
    public event EventHandler OnLeftForce;
    public event EventHandler OnBeforeForce;
    public event EventHandler OnCoinPickup;
    public event EventHandler <OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs
    {
        public State state;
    }
    public event EventHandler<OnLandedEventArgs> OnLanded;
    public class OnLandedEventArgs : EventArgs
    {
        public LandingType landingType;
        public int score;
        public float dotVector; // for angle
        public float landingSpeed;
        public float scoreMultiplier;
    }

    public enum LandingType
    {
        Success,
        WrongLandingArea,
        TooSteepAngle,
        TooFastLanding,
    }


    public enum State
    {
        WaitingToStart,
        Normal,
        GameOver,
    }

    private Rigidbody2D landerRigidBody2D;
    private float fuelAmount;
    private float fuelAmountMax = 10f;
    private State state;



    private void Awake()
    {
        Instance = this;

        fuelAmount = fuelAmountMax;
        state = State.WaitingToStart;

        landerRigidBody2D = GetComponent<Rigidbody2D>();
        landerRigidBody2D.gravityScale = 0f;
    }

    private void FixedUpdate()
    {
        OnBeforeForce?.Invoke(this, EventArgs.Empty);


        switch (state)
        {
            default:
                case State.WaitingToStart:
                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    // Pressing any input
                    landerRigidBody2D.gravityScale = GRAVITY_NORMAL;
                    SetState(State.Normal);

                }
                break;
                case State.Normal:
                if (fuelAmount <= 0f)
                {
                    // no fuel
                    return;
                }

                if (Keyboard.current.upArrowKey.isPressed || Keyboard.current.leftArrowKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                {
                    // Pressing any input
                    ConsumeFuel();
                }

                if (Keyboard.current.upArrowKey.isPressed)
                {
                    float force = 700f;
                    landerRigidBody2D.AddForce(force * transform.up * Time.deltaTime);


                    OnUpForce?.Invoke(this, EventArgs.Empty);
                }

                if (Keyboard.current.leftArrowKey.isPressed)
                {
                    float turnSpeed = +100f;
                    landerRigidBody2D.AddTorque(turnSpeed * Time.deltaTime);

                    OnLeftForce?.Invoke(this, EventArgs.Empty);
                }

                if (Keyboard.current.rightArrowKey.isPressed)
                {
                    float turnSpeed = -100f;
                    landerRigidBody2D.AddTorque(turnSpeed * Time.deltaTime);

                    OnRightForce?.Invoke(this, EventArgs.Empty);
                }
                break;

            case State.GameOver:
                break;
        }
        
        

    }

    private void OnCollisionEnter2D(Collision2D collision2D)
    {
        // this will return true of gameobject object has landingPad Component of type LandingPad
        if (!collision2D.gameObject.TryGetComponent(out LandingPad landingPad))
        {
            Debug.Log("Crashed!!");
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.TooFastLanding,
                dotVector = 0f,
                landingSpeed = 0,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        float softLandingVelocityMagnitude = 4f;
        float relativeVelocityMagnitude = collision2D.relativeVelocity.magnitude;
        if (relativeVelocityMagnitude > softLandingVelocityMagnitude)
        {
            Debug.Log("Landed too fast");
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.WrongLandingArea,
                dotVector = 0f,
                landingSpeed = relativeVelocityMagnitude,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        float dotVector = Vector2.Dot(Vector2.up, transform.up);
        float minDotVector = .90f;
        if (dotVector < minDotVector)
        {
            Debug.Log("Landed on a too steep angle!");
            OnLanded?.Invoke(this, new OnLandedEventArgs
            {
                landingType = LandingType.TooFastLanding,
                dotVector = dotVector,
                landingSpeed = relativeVelocityMagnitude,
                scoreMultiplier = 0,
                score = 0,
            });
            SetState(State.GameOver);
            return;
        }

        Debug.Log("Successful Landing");

        float maxScoreAmountLandingAngle = 100;
        float scoreDotVectorMultiplier = 10f;
        float landingAngleScore = maxScoreAmountLandingAngle - Mathf.Abs(dotVector - 1f) * scoreDotVectorMultiplier * maxScoreAmountLandingAngle;

        float maxScoreAmountLandingSpeed = 100;
        float landingSpeedScore = (softLandingVelocityMagnitude - relativeVelocityMagnitude) * maxScoreAmountLandingSpeed;

        Debug.Log("landingAngleScore: " + landingAngleScore);
        Debug.Log("landingspeedScore: " + landingSpeedScore);

        int score = Mathf.RoundToInt((landingAngleScore + landingSpeedScore) * landingPad.getScoreMultiplier());

        Debug.Log("score: " + score);
        OnLanded?.Invoke(this, new OnLandedEventArgs
        {
            landingType = LandingType.Success,
            dotVector = dotVector,
            landingSpeed = relativeVelocityMagnitude,
            scoreMultiplier = landingPad.getScoreMultiplier(),
            score = score,
        });
        SetState(State.GameOver);
    }


    private void OnTriggerEnter2D(Collider2D collider2D)
    {
        if (collider2D.gameObject.TryGetComponent(out FuelPickup fuelPickup))
        {
            float addFuelAmount = 10f;
            fuelAmount += addFuelAmount;
            if (fuelAmount > fuelAmountMax)
            {
                fuelAmount = fuelAmountMax;
            }

            fuelPickup.DestroySelf();
        }

        if (collider2D.gameObject.TryGetComponent(out CoinPickup coinPickup))
        {
           OnCoinPickup?.Invoke(this, EventArgs.Empty);
            coinPickup.DestroySelf();
        }
    }


    private void SetState(State state)
    {
        this.state = state;
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs
        {
            state = state
        });
    }

    private void ConsumeFuel()
    {
        float fuelConsumptionAmount = 1f;
        fuelAmount -= fuelConsumptionAmount * Time.deltaTime;
    }


    public float GetFuel()
    {
        return fuelAmount;
    }

    public float GetFuelAmountNormalized()
    {
        // For fuel percentage for fuel bar
        return fuelAmount / fuelAmountMax;
    }


    public float GetSpeedX()
    {
        return landerRigidBody2D.linearVelocityX;
    }

    public float GetSpeedY()
    {
        return landerRigidBody2D.linearVelocityY;
    }



}

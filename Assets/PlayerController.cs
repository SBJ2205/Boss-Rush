using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // --- SETTINGS ---
    [Header("Form Stats")]
    public float swordSpeed = 8f;   // Consistent speed for Sword
    public float swordJump = 12f;
    
    public float tankSpeed = 4f;    // Consistent speed for Tank
    public float tankJump = 5f;

    // --- ASSIGN IN INSPECTOR ---
    [Header("Forms")]
    public GameObject swordForm; 
    public GameObject tankForm; 

    // --- PRIVATE VARIABLES ---
    private Rigidbody2D currentRb; 
    private bool isSwordActive = true; 
    
    // We store the current stats in these temporary variables
    private float currentSpeed;
    private float currentJump;

    // Add a reference to the health script at the top of the class
    private Health myHealth;

    void Start()
    {
        // 1. SETUP INITIAL STATE (SWORD)
        swordForm.SetActive(true);
        tankForm.SetActive(false);
        
        currentRb = swordForm.GetComponent<Rigidbody2D>();
        
        // 2. APPLY SWORD STATS IMMEDIATELY
        currentSpeed = swordSpeed;
        currentJump = swordJump;

        // Grab the health script from the same object
        myHealth = GetComponent<Health>();
    }

    

    void Update()
    {
        // 1. DETERMINE STATS
        float activeSpeed = isSwordActive ? swordSpeed : tankSpeed;
        float activeJump = isSwordActive ? swordJump : tankJump;

        // 2. CHECK BLOCKING (Only for Tank!)
        // If we are NOT sword (meaning we are Tank) AND holding K
        if (!isSwordActive && Input.GetKey(KeyCode.K))
        {
            myHealth.isBlocking = true;
            
            // FREEZE MOVEMENT WHILE BLOCKING
            // We set velocity to (0, current Y) so you stop sliding but still fall if in air
            currentRb.linearVelocity = new Vector2(0, currentRb.linearVelocity.y);
            
            // Visual feedback: Turn the Tank slightly Darker Blue
            tankForm.GetComponent<SpriteRenderer>().color = Color.cyan;
            
            return; // STOP HERE! Don't run movement code below.
        }
        else
        {
            // Stop blocking
            if(myHealth != null) myHealth.isBlocking = false;
            
            // Reset Color (Optional fix to make sure he turns back to blue)
            if(!isSwordActive) tankForm.GetComponent<SpriteRenderer>().color = Color.blue; 
        }

        // 3. INPUTS & SWITCHING
        // Only allow switching if NOT blocking
        if (Input.GetKeyDown(KeyCode.Q))
        {
            SwitchForm();
        }

        // 4. MOVEMENT
        float moveInput = Input.GetAxisRaw("Horizontal");
        currentRb.linearVelocity = new Vector2(moveInput * activeSpeed, currentRb.linearVelocity.y);

        // 5. JUMPING
        if (Input.GetButtonDown("Jump") && Mathf.Abs(currentRb.linearVelocity.y) < 0.01f)
        {
            currentRb.AddForce(Vector2.up * activeJump, ForceMode2D.Impulse);
        }
    }

    void SwitchForm()
    {
        // Save position
        Vector3 currentPos = currentRb.transform.position;
        // Save current vertical velocity (falling speed) so you don't stop falling mid-air
        Vector2 savedVelocity = currentRb.linearVelocity;

        if (isSwordActive)
        {
            // Switch to TANK
            swordForm.SetActive(false);
            tankForm.SetActive(true);
            
            tankForm.transform.position = currentPos; 
            currentRb = tankForm.GetComponent<Rigidbody2D>(); 
            
            // Transfer velocity so movement is smooth
            currentRb.linearVelocity = savedVelocity;

            // Update Stats
            currentSpeed = tankSpeed;
            currentJump = tankJump;
        }
        else
        {
            // Switch to SWORD
            tankForm.SetActive(false);
            swordForm.SetActive(true);
            
            swordForm.transform.position = currentPos;
            currentRb = swordForm.GetComponent<Rigidbody2D>();

            // Transfer velocity
            currentRb.linearVelocity = savedVelocity;

            // Update Stats
            currentSpeed = swordSpeed;
            currentJump = swordJump;
        }

        isSwordActive = !isSwordActive;
    }
}
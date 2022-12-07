using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class RubyController : MonoBehaviour
{
    public float speed = 3.0f;
    public int level = 1;
    
    public int maxHealth = 5;
    
    public GameObject projectilePrefab;
    
    public GameObject hitEffect, eatEffect, winObj, loseObj, bkgMusic, fastObj;

    public Text fixedText, ammoText, fastText;
    
    public int health { get { return currentHealth; }}
    int currentHealth;
    
    public float timeInvincible = 2.0f;
    bool isInvincible;
    float invincibleTimer;
    
    Rigidbody2D rigidbody2d;
    float horizontal;
    float vertical;

    int numFixed = 0;
    int ammo = 4;
    int seconds;
    float speedTime;
    bool gameOver, fast;
    Animator animator;
    Vector2 lookDirection = new Vector2(1,0);
    
    AudioSource audioSource;
    public AudioClip cogClip, hitClip, reloadClip, eatClip;

    // Start is called before the first frame update
    void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        
        currentHealth = maxHealth;
        audioSource= GetComponent<AudioSource>();

        CheckFixed();
    }

    // Update is called once per frame
    void Update()
    {
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");
        
        Vector2 move = new Vector2(horizontal, vertical);
        
        if(!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            lookDirection.Set(move.x, move.y);
            lookDirection.Normalize();
        }
        
        animator.SetFloat("Look X", lookDirection.x);
        animator.SetFloat("Look Y", lookDirection.y);
        animator.SetFloat("Speed", move.magnitude);
        
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer < 0)
                isInvincible = false;
        }
        
        if(Input.GetKeyDown(KeyCode.C))
        {
            if(ammo>0)
            {
                ammo--;
                ammoText.text = ammo.ToString();
                Launch();
                PlaySound(cogClip);
            }
            
            //Invoke("CheckFixed", .3f);
            //Invoke("CheckFixed", .75f);
            //Invoke("CheckFixed", 2f);
        }
        CheckFixed();
        if (Input.GetKeyDown(KeyCode.X))
        {
            RaycastHit2D hit = Physics2D.Raycast(rigidbody2d.position + Vector2.up * 0.2f, lookDirection, 1.5f, LayerMask.GetMask("NPC"));
            if (hit.collider != null)
            {
                    NonPlayerCharacter character = hit.collider.GetComponent<NonPlayerCharacter>();
                    if (character != null)
                    {
                        if(gameOver)
                        {
                            SceneManager.LoadScene(1);
                        }
                        else
                        {
                            character.DisplayDialog();
                        }
                        
                    }  
            }
        }

        IShowSpeed();
    }
    
    void FixedUpdate()
    {
        Vector2 position = rigidbody2d.position;
        position.x = position.x + speed * horizontal * Time.deltaTime;
        position.y = position.y + speed * vertical * Time.deltaTime;

        rigidbody2d.MovePosition(position);
    }

    public void ChangeHealth(int amount)
    {
        GameObject effect = eatEffect;
        if (amount < 0)
        {
            if (isInvincible)
                return;
            
            isInvincible = true;
            invincibleTimer = timeInvincible;
            animator.SetTrigger("Hit");
            PlaySound(hitClip);
            effect = hitEffect;
        }
 
        StartCoroutine(SpawnEffect(effect));
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        UIHealthBar.instance.SetValue(currentHealth / (float)maxHealth);
    }
    
    void Launch()
    {
        GameObject projectileObject = Instantiate(projectilePrefab, rigidbody2d.position + Vector2.up * 0.5f, Quaternion.identity);

        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(lookDirection, 300);

        animator.SetTrigger("Launch");
    }

    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }

    IEnumerator SpawnEffect(GameObject g)
    {
        GameObject toDestroy = Instantiate(g, rigidbody2d.position + Vector2.up, Quaternion.identity);
        yield return new WaitForSeconds(3f);
        Destroy(toDestroy);
    }
    private void CheckFixed()
    {
        numFixed = 0;
        GameObject[] bots = GameObject.FindGameObjectsWithTag("Robot");
        foreach (GameObject bot in bots)
        {
            if(!bot.gameObject.GetComponent<EnemyController>().isBroken())
                numFixed++;
        }

        fixedText.text = "Robots Fixed: " + numFixed + "/4";

        if(numFixed == 4)
        {
            gameOver = true;
            
            winObj.SetActive(true);
            if(level == 2)
                bkgMusic.SetActive(false);
              
            if(Input.GetKeyDown(KeyCode.R) && level == 2)
            {
                
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
        if(health < 1)
        {
            gameOver = true;
            bkgMusic.SetActive(false);
            loseObj.SetActive(true);
            speed = 0;
            if(Input.GetKeyDown(KeyCode.R))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    private void OnCollisionEnter2D(Collision2D other) {
        if(other.gameObject.tag == "Ammo")
        {
            ammo+=4;
            Destroy(other.gameObject);
            ammoText.text = ammo.ToString();
            PlaySound(reloadClip);
        }
        if(other.gameObject.tag == "Boots")
        {
            StartCoroutine(SpeedBoost());
            Destroy(other.gameObject);
        }
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        if(other.gameObject.tag == "Mud")
        {
            speed /=2;
        }
    }
    private void OnTriggerExit2D(Collider2D other) {
        if(other.gameObject.tag == "Mud")
        {
            speed = 3;
        }
    }
    IEnumerator SpeedBoost()
    {
        speed = speed*2.2f;
        fast = true;
        speedTime = 6;
        yield return new WaitForSeconds(5f);

        speed = 3;
        fast = false;
    }
    private void IShowSpeed()
    {
        if(fast)
        {
            if(speedTime < 0)
            {
                fastObj.SetActive(false);
                return;
            }
            fastObj.SetActive(true);  
            speedTime-=Time.deltaTime;
            if(speedTime%60 == 0)
                seconds--;

            fastText.text = (int)speedTime + " Seconds left of speed";
        }
        if(!fast)
        {
            fastObj.SetActive(false);
            return;
        }
    }
    
}
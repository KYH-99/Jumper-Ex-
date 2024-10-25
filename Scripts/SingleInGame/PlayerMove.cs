using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMove : MonoBehaviour
{
    public float maxSpeed;
    public float jumpPower;
    public float jumpCount;
    public GameManager gameManager;

    Rigidbody2D rigid;
    SpriteRenderer spriteRenderer;
    Animator anim;
    private AudioSource audioSource;
    public AudioClip jumpClip;
    public AudioClip doubleJumpClip;
    public AudioClip damagedClip; // 추가

    void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        anim = GetComponent<Animator>();
        audioSource = gameObject.AddComponent<AudioSource>();

        jumpClip = Resources.Load<AudioClip>("MultiGameSound/JumpClip");
        doubleJumpClip = Resources.Load<AudioClip>("MultiGameSound/DoubleJumpClip");
        damagedClip = Resources.Load<AudioClip>("MultiGameSound/DamagedClip"); // 추가

        if (jumpClip == null)
        {
            Debug.LogError("JumpClip not found in Resources/MultiGameSound");
        }
        if (doubleJumpClip == null)
        {
            Debug.LogError("DoubleJumpClip not found in Resources/MultiGameSound");
        }
        if (damagedClip == null)
        {
            Debug.LogError("DamagedClip not found in Resources/MultiGameSound");
        }
    }

    private void Update()
    {
        // 점프
        if (Input.GetButtonDown("Jump") && jumpCount < 2)
        {
            rigid.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            jumpCount++;
            anim.SetTrigger(jumpCount == 1 ? "isJumping" : "isDoubleJumping");
            anim.SetBool("isJumping", true);

            if (jumpCount == 1)
            {
                PlayJumpSound();
            }
            else if (jumpCount == 2)
            {
                PlayDoubleJumpSound();
            }
        }

            //멈추는속도
        if (Input.GetButtonUp("Horizontal")) { 
            rigid.velocity = new Vector2(rigid.velocity.normalized.x * 0.5f, rigid.velocity.y);
        }

        //방향전환
        if (Input.GetButton("Horizontal"))
            spriteRenderer.flipX = Input.GetAxisRaw("Horizontal") == -1;

        //애니메이션
        if (Mathf.Abs(rigid.velocity.x) < 0.5)
            anim.SetBool("isWalking", false);
        else
            anim.SetBool("isWalking", true);
    }
    void FixedUpdate()
    {
        //움직임 컨트롤
        float h = Input.GetAxisRaw("Horizontal");
        rigid.AddForce(Vector2.right * h, ForceMode2D.Impulse);

        //최대속도
        if (rigid.velocity.x > maxSpeed) 
            rigid.velocity = new Vector2(maxSpeed, rigid.velocity.y); // Right Max Speed
        else if (rigid.velocity.x < maxSpeed * (-1))
            rigid.velocity = new Vector2(maxSpeed * (-1), rigid.velocity.y); // Left Max Speed

        //플랫폼 확인
        if (rigid.velocity.y < 0) { 
             Debug.DrawRay(rigid.position, Vector3.down, new Color(0, 1, 0));
             RaycastHit2D rayHit = Physics2D.Raycast(rigid.position, Vector3.down, 1, LayerMask.GetMask("Platform"));
             if (rayHit.collider != null) {            
                if (rayHit.distance < 0.5f)
                {
                    jumpCount = 0;

                }
                    anim.SetBool("isJumping", false);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Enemy"){
            //몬스터 공격
            if(rigid.velocity.y < 0 && transform.position.y > collision.transform.position.y){
                OnAttack(collision.transform);
            }
            else //데미지
                OnDamaged(collision.transform.position);
        }
        //스파이크 밟을시 데미지
        if(collision.gameObject.tag == "Spike"){
            OnDamaged(collision.transform.position);
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Finish"){
            //Next Stage
            gameManager.NextStage();
            PlayerReposition();
        }
    }    

    void OnAttack(Transform enemy)
    {
        //점수

        //반발력
        rigid.AddForce(Vector2.up * 5, ForceMode2D.Impulse);

        //몬스터 사망
        EnemyMove enemyMove = enemy.GetComponent<EnemyMove>();
        enemyMove.OnDamaged();
    }

    public void OnDamaged(Vector2 targetPos)
    {
        //피 다운
        gameManager.health--;

        //레이어 바꿈
        gameObject.layer = 11;

        //데미지시 캐릭터 색 바뀜
        spriteRenderer.color = new Color(1,0,0,0.6f);

        //데미지 리액션
        //시작위치로 리스폰
        PlayDamagedSound(); // 추가: 데미지 사운드 재생
        PlayerReposition();

        Invoke("OffDamaged",2);
    }

    public void OffDamaged()
    {
        //레이어 변경, 무적해제
        gameObject.layer = 10;
        spriteRenderer.color = new Color(1,1,1,1);
    }

    public void PlayerReposition()
    {
        //시작 위치값
        gameObject.transform.position = new Vector3(-5.5f,-0.5f,-1);
        jumpCount = 0;
        VelocityZero();
    }

    public void VelocityZero()
    {
        //플레이어 속도 초기화
        rigid.velocity =Vector2.zero;
    }

    private void PlayJumpSound()
    {
        if (audioSource != null && jumpClip != null)
        {
            audioSource.PlayOneShot(jumpClip, 0.2f);
            Debug.Log("Playing Jump sound.");
        }
        else
        {
            Debug.LogError("AudioSource or JumpClip is missing.");
        }
    }

    private void PlayDoubleJumpSound()
    {
        if (audioSource != null && doubleJumpClip != null)
        {
            audioSource.PlayOneShot(doubleJumpClip, 0.2f);
            Debug.Log("Playing Double Jump sound.");
        }
        else
        {
            Debug.LogError("AudioSource or DoubleJumpClip is missing.");
        }
    }

    private void PlayDamagedSound() // 추가: 데미지 사운드 재생 메서드
    {
        if (audioSource != null && damagedClip != null)
        {
            audioSource.PlayOneShot(damagedClip, 0.8f);
            Debug.Log("Playing Damaged sound.");
        }
        else
        {
            Debug.LogError("AudioSource or DamagedClip is missing.");
        }
    }
}

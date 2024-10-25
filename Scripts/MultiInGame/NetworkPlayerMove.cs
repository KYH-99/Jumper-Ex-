using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;
using Cinemachine;

public class NetworkPlayerMove : MonoBehaviourPunCallbacks, IPunObservable
{
    public bool PlayerTPC = false; // 플레이어 트리플점프 체크용 값
    public int RandEvent = 0; // 랜덤이벤트값 생성 변수
    public float maxSpeed = 6f; // 최대 속도
    public float jumpPower = 16f; // 점프 힘
    public Rigidbody2D RB; // 리지드바디 컴포넌트
    public Animator AN; // 애니메이터 컴포넌트
    public SpriteRenderer SR; // 스프라이트 렌더러 컴포넌트
    public PhotonView PV; // 포톤 뷰 컴포넌트
    public Text NickNameText; // 닉네임 텍스트 UI
    public CinemachineVirtualCamera CM; // 시네머신 가상 카메라

    private bool Platform; // 플랫폼 여부
    private Vector3 curPos; // 현재 위치
    public int jumpCount = 0; // 점프 횟수
    private NetworkManager networkManager;

    private NetworkGameManager gameManager; // 게임 매니저 참조
    private Collider2D playerCollider;
    private AudioSource audioSource; // 추가
    public AudioClip jumpClip; // 추가
    public AudioClip doubleJumpClip; // 추가
    public AudioClip damagedClip; // 추가

    private bool eventApplied = false; // 이벤트가 적용되었는지 확인하는 플래그

    void Start()
    {
        networkManager = FindObjectOfType<NetworkManager>();
        playerCollider = GetComponent<Collider2D>(); // Collider2D 컴포넌트 가져오기

        // 5초 후에 PlayerEvent() 메서드 호출
        Invoke("ApplyPlayerEvent", 5f);
    }

    void ApplyPlayerEvent()
    {
        if (!eventApplied)
        {
            RandEvent = Random.Range(0, 3); //플레이어 이벤트값 랜덤생성 후 저장
            PlayerEvent(); // 이벤트 적용
            eventApplied = true; // 이벤트 적용 플래그 설정

            /// 자신의 클라이언트에서만 패널 활성화
            ShowEventPanel(RandEvent);
        }
    }

    [PunRPC]
    void ShowEventPanel(int eventIndex)
    {
        if (!PV.IsMine) return; // 자신의 이벤트가 아니면 반환

        networkManager.ShowEventPanel(eventIndex); // 각 플레이어에게 자신이 해당하는 패널만 표시
    }

    //private IEnumerator HideEventPanelAfterDelay(int eventIndex, float delay)
    //{
        //yield return new WaitForSeconds(delay);

        //switch (eventIndex)
        //{
            //case 0:
                //networkManager.tripleJumpPanel.SetActive(false);
                //break;
            //case 1:
                //networkManager.doubleJumpPowerPanel.SetActive(false);
               // break;
            //case 2:
                //networkManager.maxSpeedPanel.SetActive(false);
                //break;
        //}
    //}

    void Awake()
    {
        RB = GetComponent<Rigidbody2D>();
        AN = GetComponent<Animator>();
        SR = GetComponent<SpriteRenderer>();
        PV = GetComponent<PhotonView>();
        gameManager = FindObjectOfType<NetworkGameManager>(); // 게임 매니저 찾기

        // 닉네임 설정
        NickNameText.text = PV.IsMine ? PhotonNetwork.NickName : PV.Owner.NickName;
        NickNameText.color = PV.IsMine ? Color.green : Color.red;

        if (PV.IsMine)
        {
            var CM = GameObject.Find("CMCamera").GetComponent<CinemachineVirtualCamera>();
            CM.Follow = transform;
            CM.LookAt = transform;
            Debug.Log("Camera assigned to player: " + PhotonNetwork.NickName);
        }

        // AudioSource 설정
        audioSource = gameObject.AddComponent<AudioSource>();
        jumpClip = Resources.Load<AudioClip>("MultiGameSound/JumpClip");
        doubleJumpClip = Resources.Load<AudioClip>("MultiGameSound/DoubleJumpClip");
        damagedClip = Resources.Load<AudioClip>("MultiGameSound/DamagedClip");

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

    void Update()
    {

        // 네트워크 매니저에서 채팅 입력 필드의 활성화 상태를 확인합니다.
        if (PV.IsMine && networkManager != null && networkManager.IsChatInputActive())
        {
            return; // 채팅 입력 필드가 활성화된 동안에는 이동 및 점프를 무시합니다.
        }
        
        if (PV.IsMine)
        {
            // ← → 이동
            float axis = Input.GetAxisRaw("Horizontal");
            RB.velocity = new Vector2(maxSpeed * axis, RB.velocity.y);

            // 이동 애니메이션 설정
            if (axis != 0)
            {
                AN.SetBool("isWalking", true);
                PV.RPC("FlipXRPC", RpcTarget.AllBuffered, axis);
            }
            else AN.SetBool("isWalking", false);

            // 바닥 체크
            Platform = Physics2D.OverlapCircle((Vector2)transform.position + new Vector2(0, -0.5f), 0.07f, 1 << LayerMask.NameToLayer("Platform"));
            AN.SetBool("isJumping", !Platform);

            // 점프 및 더블 점프 처리
            if (Input.GetButtonDown("Jump"))
            {
                if (jumpCount == 0)
                {
                    RB.velocity = new Vector2(RB.velocity.x, 0);
                    RB.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                    jumpCount++;
                    AN.SetBool("isJumping", true);
                    PV.RPC("JumpRPC", RpcTarget.All, jumpCount); // 점프 동기화
                }
                else if (jumpCount == 1)
                {
                    RB.velocity = new Vector2(RB.velocity.x, 0);
                    RB.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                    jumpCount++;
                    AN.SetTrigger("isDoubleJumping");
                    AN.SetBool("isJumping", true);
                    PV.RPC("JumpRPC", RpcTarget.All, jumpCount); // 점프 동기화
                }
                else if (jumpCount == 2 && PlayerTPC == true)
                {
                    RB.velocity = new Vector2(RB.velocity.x, 0);
                    RB.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
                    jumpCount++;
                    AN.SetTrigger("isDoubleJumping");
                    AN.SetBool("isJumping", true);
                    PV.RPC("JumpRPC", RpcTarget.All, jumpCount); // 점프 동기화
                }
            }

            // 멈추는 속도 조절
            if (Input.GetButtonUp("Horizontal"))
            {
                RB.velocity = new Vector2(RB.velocity.normalized.x * 0.5f, RB.velocity.y);
            }
        }
        else if ((transform.position - curPos).sqrMagnitude >= 100) transform.position = curPos;
        else transform.position = Vector3.Lerp(transform.position, curPos, Time.deltaTime * 10);
    }

    void FixedUpdate()
    {
        if (!PV.IsMine) return;

        // 최대 속도 제한
        if (Mathf.Abs(RB.velocity.x) > maxSpeed)
            RB.velocity = new Vector2(Mathf.Sign(RB.velocity.x) * maxSpeed, RB.velocity.y);

        // 플랫폼 확인 및 점프 횟수 초기화
        if (RB.velocity.y < 0)
        {
            RaycastHit2D rayHitCenter = Physics2D.Raycast(transform.position, Vector2.down, 0.5f, LayerMask.GetMask("Platform"));
            RaycastHit2D rayHitFront = Physics2D.Raycast(transform.position + new Vector3(0.17f, 0, 0), Vector2.down, 0.55f, LayerMask.GetMask("Platform"));
            RaycastHit2D rayHitBack = Physics2D.Raycast(transform.position - new Vector3(0.17f, 0, 0), Vector2.down, 0.55f, LayerMask.GetMask("Platform"));

            Debug.DrawRay(transform.position, Vector2.down * 0.5f, Color.red);
            Debug.DrawRay(transform.position + new Vector3(0.17f, 0, 0), Vector2.down * 0.55f, Color.green);
            Debug.DrawRay(transform.position - new Vector3(0.17f, 0, 0), Vector2.down * 0.55f, Color.green);
            if (rayHitCenter.collider != null || rayHitFront.collider != null || rayHitBack.collider != null)
            {
                jumpCount = 0;
                AN.SetBool("isJumping", false);
            }
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!PV.IsMine) return;

        if (collision.gameObject.tag == "Enemy")
        {
            if (RB.velocity.y < 0 && transform.position.y > collision.transform.position.y)
            {
                OnAttack(collision.transform); // 공격 처리
            }
            else
            {
                OnDamaged(collision.transform.position); // 데미지 처리
            }
        }
        if (collision.gameObject.tag == "Spike")
        {
            OnDamaged(collision.transform.position); // 스파이크 데미지 처리
        }
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (!PV.IsMine) return;

        if (collision.gameObject.tag == "Finish")
        {
            networkManager.OnPlayerReachedFinish();
            playerCollider.enabled = false; // 물리적 충돌 비활성화
            RB.simulated = false;
            AN.enabled = false;
            SR.enabled = false;
        }
    }

    void OnAttack(Transform enemy)
    {
        RB.AddForce(Vector2.up * 5, ForceMode2D.Impulse); // 반발력 추가

        EnemyMove enemyMove = enemy.GetComponent<EnemyMove>();
        enemyMove.OnDamaged(); // 적에게 데미지 처리
    }

    public void OnDamaged(Vector2 targetPos)
    {
        if (!PV.IsMine) return;

        gameManager.health--; // 체력 감소

        gameObject.layer = 11; // 레이어 변경

        SR.color = new Color(1, 0, 0, 0.6f); // 색상 변경

        PlayDamagedSound(); // 데미지 사운드 재생

        PlayerReposition(); // 플레이어 위치 초기화

        Invoke("OffDamaged", 2); // 2초 후 무적 해제
    }

    public void OffDamaged()
    {
        gameObject.layer = 10; // 레이어 변경
        SR.color = new Color(1, 1, 1, 1); // 색상 초기화
    }

    public void PlayerReposition()
    {
        gameObject.transform.position = new Vector3(-25.91f, -9.57f, -1); // 시작 위치로 이동
        jumpCount = 0; // 점프 횟수 초기화
        VelocityZero(); // 속도 초기화
    }

    public void VelocityZero()
    {
        if (RB != null)
        {
            RB.velocity = Vector2.zero; // 속도 초기화
        }
    }

    [PunRPC]
    void FlipXRPC(float axis) => SR.flipX = axis == -1; // 방향 전환 동기화

    [PunRPC]
    void JumpRPC(int jumpState)
    {
        if (!PV.IsMine) return;

        if (jumpState == 1)
        {
            RB.velocity = new Vector2(RB.velocity.x, 0);
            RB.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            AN.SetBool("isJumping", true);
            PlayJumpSound(); // 1단 점프 사운드 재생
        }
        else if (jumpState == 2)
        {
            RB.velocity = new Vector2(RB.velocity.x, 0);
            RB.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            AN.SetTrigger("isDoubleJumping");
            AN.SetBool("isJumping", true);
            PlayDoubleJumpSound(); // 2단 점프 사운드 재생
        }
        else if (jumpState == 3 && PlayerTPC) // 3단 점프 조건 추가
        {
            RB.velocity = new Vector2(RB.velocity.x, 0);
            RB.AddForce(Vector2.up * jumpPower, ForceMode2D.Impulse);
            AN.SetTrigger("isDoubleJumping");
            AN.SetBool("isJumping", true);
            PlayJumpSound(); // 3단 점프 사운드 재생
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position); // 위치 동기화
            stream.SendNext(jumpCount); // 점프 횟수 동기화
        }
        else
        {
            curPos = (Vector3)stream.ReceiveNext(); // 위치 수신
            jumpCount = (int)stream.ReceiveNext(); // 점프 횟수 수신
        }
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

    private void PlayDamagedSound()
    {
        if (audioSource != null && damagedClip != null)
        {
            audioSource.PlayOneShot(damagedClip, 0.6f);
            Debug.Log("Playing Damaged sound.");
        }
        else
        {
            Debug.LogError("AudioSource or DamagedClip is missing.");
        }
    }

    public void PlayerEvent()
    {
        if (RandEvent == 0)
        {
            PlayerTPC = true;
        }
        else if (RandEvent == 1)
        {
            jumpPower = 32f;
        }
        else if (RandEvent == 2)
        {
            maxSpeed = 12f;
        }
    }
}

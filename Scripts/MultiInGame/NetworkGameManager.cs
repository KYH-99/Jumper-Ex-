using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine.UI;


public class NetworkGameManager : MonoBehaviourPunCallbacks
{
    public int stageIndex;
    public int health;
    public NetworkPlayerMove player;
    public GameObject[] Stages;

    public UnityEngine.UI.Image[] UIhealth; 
    public Text UIStage;
    public GameObject RestartBtn;

        [Header("ETC")]
    public PhotonView PV;
    public GameObject playerPrefab; // 네트워크 플레이어 프리팹
    private AudioSource audioSource; // 추가
    public AudioClip damagedClip; // 추가

 


    private void Start()
    {
        // 네트워크 플레이어가 아닌 경우 해당 오브젝트를 비활성화합니다.
        PV = GetComponent<PhotonView>();

    // photonView가 null이 아닌지 확인하여 NullReferenceException을 방지합니다.
        if (PV != null && !PV.IsMine)
        {
            enabled = false;
        }
    }

    void Awake()
    {
        // AudioSource 설정
        audioSource = gameObject.AddComponent<AudioSource>();
        damagedClip = Resources.Load<AudioClip>("MultiGameSound/DamagedClip");
    }


    [PunRPC]
    void ActivateStage(int index)
    {
        Stages[index].SetActive(true);
    }

    [PunRPC]
    void DeactivateStage(int index)
    {
        Stages[index].SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            collision.attachedRigidbody.velocity = Vector2.zero;
            collision.transform.position = new Vector3(-25.91f, -9.57f, -1);
            PlayDamagedSound(); // 데미지 사운드 재생
        }
    }

    private void PlayDamagedSound()
    {
        if (audioSource != null && damagedClip != null)
        {
            audioSource.PlayOneShot(damagedClip);
            Debug.Log("Playing Damaged sound.");
        }
        else
        {
            Debug.LogError("AudioSource or DamagedClip is missing.");
        }
    }

    void PlayerReposition()
    {
        if (player != null)
        {
            // 해당 부분 PlayerMove에서 가져오는걸로 고치기
            player.transform.position = new Vector3(-25.91f, -9.57f, -1);
            player.VelocityZero();
        }
    }

}
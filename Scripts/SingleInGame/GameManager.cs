
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public int stageIndex;
    public int health;
    public PlayerMove player;
    public GameObject[] Stages;

    public UnityEngine.UI.Image[] UIhealth; 
    public Text UIStage;
    public GameObject RestartBtn;
    private AudioSource audioSource;
    public AudioClip damagedClip; // 추가

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        damagedClip = Resources.Load<AudioClip>("MultiGameSound/DamagedClip"); // 추가
    }

    public void NextStage()
    {
        //change stage
        if(stageIndex < Stages.Length-1){
            Stages[stageIndex].SetActive(false);
            stageIndex++;
            Stages[stageIndex].SetActive(true);
            PlayerReposition();

            UIStage.text = "STAGE" + (stageIndex+1);
        }
        else{//게임 클리어
            //Player Controll Lock
            Time.timeScale = 0;
            //결과 창
            Debug.Log("게임 클리어!");
        }
    }
    
    void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.tag == "Player"){
            collision.attachedRigidbody.velocity = Vector2.zero;
            collision.transform.position = new Vector3(-5.5f,-0.5f,0-1);
            PlayDamagedSound(); // 추가: 데미지 사운드 재생
        }
        
    }

    void PlayerReposition()
    {
        if (player != null){
        //해당부분 PlayerMove에서 가져오는걸로 고치기
        player.transform.position = new Vector3(-5.5f,-0.5f,-1);
        player.VelocityZero();
        }
    }

    private void PlayDamagedSound() // 추가: 데미지 사운드 재생 메서드
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
}

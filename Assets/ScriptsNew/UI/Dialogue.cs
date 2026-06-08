using System.Collections;
using UnityEngine;
using TMPro;

public class Dialogue : MonoBehaviour
{
    public bool IsActive => gameObject.activeSelf;
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private string[] lines;
    [SerializeField] private float textSpeed = 0.05f;

    private int index;
    private bool dialogueStarted = false;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip voiceBlip;
    [Range(0.1f, 1f)] [SerializeField] private float pitchVariation = 0.1f;

    void Start()
    {
        StartDialogue();
    }

    void Update()
    {
        if (!dialogueStarted)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            if (textComponent.text == lines[index])
            {
                NextLine();
            }
            else
            {
                StopAllCoroutines();
                textComponent.text = lines[index];
            }
        }
    }

    // CALLED BY TRIGGER
    public void StartDialogue()
    {
        gameObject.SetActive(true);
        dialogueStarted = true;

        index = 0;
        textComponent.text = string.Empty;
        StartCoroutine(TypeLine());
    }

    IEnumerator TypeLine()
    {
        foreach (char c in lines[index].ToCharArray())
        {
            textComponent.text += c;

            if (!char.IsWhiteSpace(c))
            {
                PlayBlip();
            }

            yield return new WaitForSeconds(textSpeed);
        }
    }

    void NextLine()
    {
        if (index < lines.Length - 1)
        {
            index++;
            textComponent.text = string.Empty;
            StartCoroutine(TypeLine());
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        StopAllCoroutines();
        dialogueStarted = false;
        gameObject.SetActive(false);
    }
    void PlayBlip()
    {
        if (audioSource && voiceBlip)
        {
            audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
            audioSource.PlayOneShot(voiceBlip);
        }
    }
}
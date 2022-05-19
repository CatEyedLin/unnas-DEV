using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class Card : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
{
    public enum Colors
    {
        wild, blue, green, red, yellow   //ColorAdd add symbols for colors
    }
    public enum Contents
    {
        zero, one, two, three, four, five, six, seven, eight, nine, 
        skip, reverse, drawTwo, wild, wildDrawFour
    }

    public Colors color;
    public Contents content;

    public Image img;
    //public Button button;

    //public enum 

    public bool grow;
    int orignalSiblingIndex;

    float orignalYpos;

    GameManager gameManager;

    public Vector2 wantedAnchoredPosition;
    public Vector3 wantedEulerAngles;
    public Quaternion wantedRotation;

    RectTransform rect;

    public bool discarding;

    // Start is called before the first frame update
    void Start()
    {
        rect = gameObject.GetComponent<RectTransform>();
        wantedAnchoredPosition = rect.anchoredPosition;
        orignalSiblingIndex = rect.GetSiblingIndex();
        orignalYpos = rect.anchoredPosition.y;
        gameManager = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<GameManager>();
        img = gameObject.GetComponent<Image>();
       // button = gameObject.GetComponent<Button>();

        img.sprite = gameManager.GetCardSprite(this);

    }

    // Update is called once per frame
    void Update()
    {
        if (grow)
        {
            if (gameObject.GetComponent<RectTransform>().sizeDelta.y < 130)
            {
                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, gameObject.GetComponent<RectTransform>().sizeDelta.y + 500 * Time.deltaTime);
            }
            if (gameObject.GetComponent<RectTransform>().sizeDelta.y > 130) { gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 130); }
            //----------s
            if (false)
            {
                if (gameObject.GetComponent<RectTransform>().anchoredPosition.y > gameObject.GetComponent<RectTransform>().anchoredPosition.y - 1000 || true)
                {
                    gameObject.GetComponent<RectTransform>().anchoredPosition.Set(gameObject.GetComponent<RectTransform>().anchoredPosition.x,
                                                                               gameObject.GetComponent<RectTransform>().anchoredPosition.y - 500 * Time.deltaTime);
                }
                else
                {
                    gameObject.GetComponent<RectTransform>().anchoredPosition.Set(gameObject.GetComponent<RectTransform>().anchoredPosition.x,
                                                                             orignalYpos - 1000);
                }
            }
        }
        else
        {
            if (gameObject.GetComponent<RectTransform>().sizeDelta.y > 120)
            {
                gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, gameObject.GetComponent<RectTransform>().sizeDelta.y - 1500 * Time.deltaTime);
            }
            if (gameObject.GetComponent<RectTransform>().sizeDelta.y < 120) { gameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 120); }
        }

        if (discarding && Vector2.Distance(wantedAnchoredPosition, rect.anchoredPosition) < 0.05)
        {
            gameManager.discard.img.sprite = gameManager.GetCardSprite(gameManager.discard);
            Destroy(this.gameObject);
        }

        rect.anchoredPosition = Vector2.Lerp(rect.anchoredPosition, wantedAnchoredPosition, 14f * Time.deltaTime);
        wantedRotation = Quaternion.Euler(wantedEulerAngles.x, wantedEulerAngles.y, wantedEulerAngles.z);
        rect.rotation = Quaternion.Lerp(rect.rotation, wantedRotation, 14f * Time.deltaTime);
    }

    public override string ToString()
    {
        return color.ToString() + ":" + content.ToString();
    }
    public string ToSending()
    {
        return color.GetHashCode() + ":" + content.GetHashCode();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (this.name == "Discard pile") { }
        else
        {
            gameObject.GetComponent<RectTransform>().SetSiblingIndex(1000);
            grow = true;
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (this.name == "Discard pile") { }
        else
        {
            gameObject.GetComponent<RectTransform>().SetSiblingIndex(gameManager.hand.FindIndex(o => o == this.gameObject));
            // gameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(gameObject.GetComponent<RectTransform>().anchoredPosition.x,
            //                                                     orignalYpos);
            grow = false;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            gameManager.PlayCard(this.gameObject);
        }
    }
}

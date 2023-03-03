using UnityEngine.UIElements;

public class CustomComment : VisualElement
{
    public TextField textElem;

    public CustomComment(string comment = "...")
    {
        textElem = new TextField();
        textElem.tripleClickSelectsLine = true;
        textElem.multiline = true;
        textElem.style.flexDirection = FlexDirection.Column;
        textElem.style.flexGrow = 1;
        textElem.style.flexWrap = Wrap.Wrap;
        //textElem.style.height = textElem.contentContainer.style.height;
        textElem.style.height = new StyleLength(200.0f);
        textElem.value = comment;
        Add(textElem);
    }
}

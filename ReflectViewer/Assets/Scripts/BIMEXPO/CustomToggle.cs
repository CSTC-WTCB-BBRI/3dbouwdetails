using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

class CustomToggle : VisualElement
{
    public new class UxmlFactory : UxmlFactory<CustomToggle, UxmlTraits> { }

    public new class UxmlTraits : VisualElement.UxmlTraits
    {
        UxmlFloatAttributeDescription m_Float = new UxmlFloatAttributeDescription { name = "float-attr", defaultValue = 0.1f };
        UxmlColorAttributeDescription m_Color = new UxmlColorAttributeDescription { name = "color-attr", defaultValue = Color.red };

        public override IEnumerable<UxmlChildElementDescription> uxmlChildElementsDescription
        {
            get { yield break; }
        }

        public override void Init(VisualElement ve, IUxmlAttributes bag, CreationContext cc)
        {
            base.Init(ve, bag, cc);

            var ate = ve as CustomToggle;

            ate.Clear();
            Toggle tt = new Toggle();
            //Label l = t.Q<Label>();
            //l.style.color = m_Color.GetValueFromBag(bag, cc);
            //t.Q<>
            ate.Add(new Toggle("test"));
            //tt = ate.Q<Toggle>("test");
            //tt.labelElement.style.color = Color.red;
            //ate.floatAttr = m_Float.GetValueFromBag(bag, cc);
            //ate.Add(new FloatField("Float") { value = ate.floatAttr });
            /*
            ate.colorAttr = m_Color.GetValueFromBag(bag, cc);
            ate.Add(new ColorField("Color") { value = ate.colorAttr });
            */
            //((CustomToggle)ve).floatAttr = m_Float.GetValueFromBag(bag, cc);
        }
    }

    public float floatAttr { get; set; }
    public Color colorAttr { get; set; }
}

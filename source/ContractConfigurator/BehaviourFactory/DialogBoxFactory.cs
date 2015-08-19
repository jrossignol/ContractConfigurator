using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using ContractConfigurator;
using ContractConfigurator.ExpressionParser;
namespace ContractConfigurator.Behaviour
{
    /// <summary>
    /// BehaviourFactory wrapper for DialogBox ContractBehaviour.
    /// </summary>
    public class DialogBoxFactory : BehaviourFactory
    {
        protected List<DialogBox.DialogDetail> details = new List<DialogBox.DialogDetail>();

        public override bool Load(ConfigNode configNode)
        {
            // Load base class
            bool valid = base.Load(configNode);

            int index = 0;
            foreach (ConfigNode child in ConfigNodeUtil.GetChildNodes(configNode, "DIALOG_BOX"))
            {
                DataNode childDataNode = new DataNode("DIALOG_BOX_" + index++, dataNode, this);
                try
                {
                    ConfigNodeUtil.SetCurrentDataNode(childDataNode);
                    DialogBox.DialogDetail detail = new DialogBox.DialogDetail();
                    details.Add(detail);

                    valid &= ConfigNodeUtil.ParseValue<DialogBox.TriggerCondition>(child, "condition", x => detail.condition = x, this);
                    valid &= ConfigNodeUtil.ParseValue<DialogBox.Position>(child, "position", x => detail.position = x, this, DialogBox.Position.LEFT);
                    valid &= ConfigNodeUtil.ParseValue<float>(child, "width", x => detail.width = x, this, 0.8f, x => Validation.Between(x, 0.0f, 1.0f));
                    valid &= ConfigNodeUtil.ParseValue<float>(child, "height", x => detail.height = x, this, 0.0f, x => Validation.Between(x, 0.0f, 1.0f));
                    valid &= ConfigNodeUtil.ParseValue<string>(child, "title", x => detail.title = x, this, "");
                    valid &= ConfigNodeUtil.ParseValue<Color>(child, "titleColor", x => detail.titleColor = x, this, Color.white);
                    valid &= ConfigNodeUtil.ParseValue<string>(child, "parameter", x => detail.parameter = x, this, (string)null,
                        x => ValidateMandatoryParameter(x, detail.condition));

                    foreach (ConfigNode sectionNode in child.GetNodes())
                    {
                        if (sectionNode.name == "TEXT")
                        {
                            DialogBox.TextSection section = new DialogBox.TextSection();
                            detail.sections.Add(section);

                            valid &= ConfigNodeUtil.ParseValue<string>(sectionNode, "text", x => section.text = x, this);
                            valid &= ConfigNodeUtil.ParseValue<Color>(sectionNode, "textColor", x => section.textColor = x, this, new Color(0.8f, 0.8f, 0.8f));
                            valid &= ConfigNodeUtil.ParseValue<int>(sectionNode, "fontSize", x => section.fontSize = x, this, 16);
                        }
                        else if (sectionNode.name == "IMAGE")
                        {
                            DialogBox.ImageSection section = new DialogBox.ImageSection();
                            detail.sections.Add(section);

                            valid &= ConfigNodeUtil.ParseValue<string>(sectionNode, "url", x => section.imageURL = x, this);
                            valid &= ConfigNodeUtil.ParseValue<string>(sectionNode, "characterName",
                                x => { section.characterName = x; section.showName = !string.IsNullOrEmpty(x); }, this, "");
                             
                            valid &= ConfigNodeUtil.ParseValue<Color>(sectionNode, "textColor", x => section.textColor = x, this, new Color(0.729f, 0.855f, 0.333f));
                        }
                        else if (sectionNode.name == "INSTRUCTOR")
                        {
                            DialogBox.InstructorSection section = new DialogBox.InstructorSection();
                            detail.sections.Add(section);

                            valid &= ConfigNodeUtil.ParseValue<string>(sectionNode, "name", x => section.name = x, this);
                            valid &= ConfigNodeUtil.ParseValue<bool>(sectionNode, "showName", x => section.showName = x, this, true);
                            valid &= ConfigNodeUtil.ParseValue<string>(sectionNode, "characterName", x => section.characterName = x, this, "");
                            valid &= ConfigNodeUtil.ParseValue<Color>(sectionNode, "textColor", x => section.textColor = x, this, new Color(0.729f, 0.855f, 0.333f));
                            valid &= ConfigNodeUtil.ParseValue<DialogBox.InstructorSection.Animation?>(sectionNode, "animation", x => section.animation = x, this,
                                (DialogBox.InstructorSection.Animation?)null);
                        }
                        else if (sectionNode.name == "KERBAL")
                        {
                            DialogBox.KerbalSection section = new DialogBox.KerbalSection();
                            detail.sections.Add(section);

                            valid &= ConfigNodeUtil.ParseValue<bool>(sectionNode, "showName", x => section.showName = x, this, true);
                            valid &= ConfigNodeUtil.ParseValue<string>(sectionNode, "characterName", x => section.characterName = x, this, "");
                            valid &= ConfigNodeUtil.ParseValue<Color>(sectionNode, "textColor", x => section.textColor = x, this, new Color(0.729f, 0.855f, 0.333f));

                            valid &= ConfigNodeUtil.ParseValue<int>(sectionNode, "crewIndex", x => section.crewIndex = x, this, 0);
                            valid &= ConfigNodeUtil.ParseValue<List<string>>(sectionNode, "excludeName", x => section.excludeName = x, this, new List<string>());
                        }
                        else if (sectionNode.name == "BREAK")
                        {
                            DialogBox.BreakSection section = new DialogBox.BreakSection();
                            detail.sections.Add(section);
                        }
                    }
                }
                finally
                {
                    ConfigNodeUtil.SetCurrentDataNode(dataNode);
                }
            }
            valid &= ConfigNodeUtil.ValidateMandatoryChild(configNode, "DIALOG_BOX", this);

            return valid;
        }

        protected bool ValidateMandatoryParameter(string parameter, DialogBox.TriggerCondition condition)
        {
            if (parameter == null && (condition == DialogBox.TriggerCondition.PARAMETER_COMPLETED ||
                condition == DialogBox.TriggerCondition.PARAMETER_FAILED))
            {
                throw new ArgumentException("Required if condition is PARAMETER_COMPLETED or PARAMETER_FAILED.");
            }
            return true;
        }

        public override ContractBehaviour Generate(ConfiguredContract contract)
        {
            return new DialogBox(details);
        }
    }
}

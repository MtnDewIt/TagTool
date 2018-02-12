using TagTool.Ai;
using TagTool.Serialization;
using System.Collections.Generic;

namespace TagTool.TagDefinitions
{
    [TagStructure(Name = "ai_mission_dialogue", Tag = "mdlg", Size = 0xC)]
    public class AiMissionDialogue
    {
        public List<AiMissionDialogueLine> Lines;
    }
}
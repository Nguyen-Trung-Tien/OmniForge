namespace OmniForge.Core.Common;

/// <summary>
/// Strongly-typed string constants for application-wide localization keys.
/// </summary>
public static class LocalizationKeys
{
    // Common Actions
    public const string Common_Apply = "Common_Apply";
    public const string Common_Cancel = "Common_Cancel";
    public const string Common_Confirm = "Common_Confirm";
    public const string Common_Save = "Common_Save";
    public const string Common_Close = "Common_Close";
    public const string Common_Browse = "Common_Browse";
    public const string Common_Copy = "Common_Copy";
    public const string Common_Delete = "Common_Delete";
    public const string Common_Refresh = "Common_Refresh";
    public const string Common_SearchPlaceholder = "Common_SearchPlaceholder";
    public const string Common_Loading = "Common_Loading";
    public const string Common_Processing = "Common_Processing";
    public const string Common_Success = "Common_Success";
    public const string Common_Failed = "Common_Failed";
    public const string Common_Error = "Common_Error";
    public const string Common_Dismiss = "Common_Dismiss";
    public const string Common_Undo = "Common_Undo";
    public const string Common_Redo = "Common_Redo";
    public const string Common_Version = "Common_Version";

    // Navigation & Shell
    public const string Nav_AppTitle = "Nav_AppTitle";
    public const string Nav_Dashboard = "Nav_Dashboard";
    public const string Nav_Dashboard_Desc = "Nav_Dashboard_Desc";
    public const string Nav_BatchRename = "Nav_BatchRename";
    public const string Nav_BatchRename_Desc = "Nav_BatchRename_Desc";
    public const string Nav_BatchFolder = "Nav_BatchFolder";
    public const string Nav_BatchFolder_Desc = "Nav_BatchFolder_Desc";
    public const string Nav_Checksum = "Nav_Checksum";
    public const string Nav_Checksum_Desc = "Nav_Checksum_Desc";
    public const string Nav_FileTools = "Nav_FileTools";
    public const string Nav_FileTools_Desc = "Nav_FileTools_Desc";
    public const string Nav_TextInspector = "Nav_TextInspector";
    public const string Nav_TextInspector_Desc = "Nav_TextInspector_Desc";
    public const string Nav_LuckyWheel = "Nav_LuckyWheel";
    public const string Nav_LuckyWheel_Desc = "Nav_LuckyWheel_Desc";
    public const string Nav_Settings = "Nav_Settings";

    // Settings Page
    public const string Settings_Title = "Settings_Title";
    public const string Settings_Subtitle = "Settings_Subtitle";
    public const string Settings_Appearance_Title = "Settings_Appearance_Title";
    public const string Settings_Appearance_Subtitle = "Settings_Appearance_Subtitle";
    public const string Settings_Theme = "Settings_Theme";
    public const string Settings_Theme_Default = "Settings_Theme_Default";
    public const string Settings_Theme_Light = "Settings_Theme_Light";
    public const string Settings_Theme_Dark = "Settings_Theme_Dark";
    public const string Settings_MicaBackdrop = "Settings_MicaBackdrop";
    public const string Settings_Language_Title = "Settings_Language_Title";
    public const string Settings_Language_Subtitle = "Settings_Language_Subtitle";
    public const string Settings_Language_Label = "Settings_Language_Label";
    public const string Settings_Language_Note = "Settings_Language_Note";
    public const string Settings_SavePreferences = "Settings_SavePreferences";
    public const string Settings_SavedSuccess = "Settings_SavedSuccess";
    public const string Settings_ThemeUpdatedLive = "Settings_ThemeUpdatedLive";

    // Dashboard Page
    public const string Dashboard_Title = "Dashboard_Title";
    public const string Dashboard_Subtitle = "Dashboard_Subtitle";
    public const string Dashboard_Status_Title = "Dashboard_Status_Title";
    public const string Dashboard_Status_Subtitle = "Dashboard_Status_Subtitle";
    public const string Dashboard_Welcome = "Dashboard_Welcome";
    public const string Dashboard_RegisteredTools = "Dashboard_RegisteredTools";
    public const string Dashboard_Highlights_Title = "Dashboard_Highlights_Title";
    public const string Dashboard_Highlights_Subtitle = "Dashboard_Highlights_Subtitle";
    public const string Dashboard_Highlight1 = "Dashboard_Highlight1";
    public const string Dashboard_Highlight2 = "Dashboard_Highlight2";
    public const string Dashboard_Highlight3 = "Dashboard_Highlight3";
    public const string Dashboard_Available_Title = "Dashboard_Available_Title";
    public const string Dashboard_Available_Subtitle = "Dashboard_Available_Subtitle";
    public const string Dashboard_Launch = "Dashboard_Launch";

    // Shared Controls
    public const string Control_ToolHeader_ConfirmAndExecute = "Control_ToolHeader_ConfirmAndExecute";
    public const string Control_ToolHeader_Cancel = "Control_ToolHeader_Cancel";
    public const string Control_ToolHeader_ActionCompleted = "Control_ToolHeader_ActionCompleted";
    public const string Control_ToolHeader_ActionConfirmation = "Control_ToolHeader_ActionConfirmation";
    public const string Control_StatusFooter_CancelTask = "Control_StatusFooter_CancelTask";
    public const string Control_StatusFooter_CancelTaskToolTip = "Control_StatusFooter_CancelTaskToolTip";
    public const string Control_StatusFooter_Ready = "Control_StatusFooter_Ready";
    public const string Control_StatusFooter_Processing = "Control_StatusFooter_Processing";
    public const string Control_EmptyState_NoData = "Control_EmptyState_NoData";

    // Lucky Wheel Module
    public const string LuckyWheel_Title = "LuckyWheel_Title";
    public const string LuckyWheel_Subtitle = "LuckyWheel_Subtitle";
    public const string LuckyWheel_ItemsListTitle = "LuckyWheel_ItemsListTitle";
    public const string LuckyWheel_ItemsListSubtitle = "LuckyWheel_ItemsListSubtitle";
    public const string LuckyWheel_ImportFileBtn = "LuckyWheel_ImportFileBtn";
    public const string LuckyWheel_ImportFileToolTip = "LuckyWheel_ImportFileToolTip";
    public const string LuckyWheel_Presets = "LuckyWheel_Presets";
    public const string LuckyWheel_Actions = "LuckyWheel_Actions";

    public const string LuckyWheel_Preset_Decision = "LuckyWheel_Preset_Decision";
    public const string LuckyWheel_Preset_Lunch = "LuckyWheel_Preset_Lunch";
    public const string LuckyWheel_Preset_Raffle = "LuckyWheel_Preset_Raffle";
    public const string LuckyWheel_Preset_Standup = "LuckyWheel_Preset_Standup";
    public const string LuckyWheel_Preset_Dice = "LuckyWheel_Preset_Dice";

    public const string LuckyWheel_Action_SortName = "LuckyWheel_Action_SortName";
    public const string LuckyWheel_Action_SortWeight = "LuckyWheel_Action_SortWeight";
    public const string LuckyWheel_Action_Shuffle = "LuckyWheel_Action_Shuffle";
    public const string LuckyWheel_Action_Equalize = "LuckyWheel_Action_Equalize";
    public const string LuckyWheel_Action_EnableAll = "LuckyWheel_Action_EnableAll";
    public const string LuckyWheel_Action_DisableAll = "LuckyWheel_Action_DisableAll";
    public const string LuckyWheel_Action_CopyCsv = "LuckyWheel_Action_CopyCsv";
    public const string LuckyWheel_Action_ClearAll = "LuckyWheel_Action_ClearAll";

    public const string LuckyWheel_QuickAdd_Placeholder = "LuckyWheel_QuickAdd_Placeholder";
    public const string LuckyWheel_QuickAdd_AddBtn = "LuckyWheel_QuickAdd_AddBtn";
    public const string LuckyWheel_QuickAdd_AddToolTip = "LuckyWheel_QuickAdd_AddToolTip";
    public const string LuckyWheel_Filter_Placeholder = "LuckyWheel_Filter_Placeholder";
    public const string LuckyWheel_Filter_Header = "LuckyWheel_Filter_Header";
    public const string LuckyWheel_Badge_Active = "LuckyWheel_Badge_Active";
    public const string LuckyWheel_Badge_Weight = "LuckyWheel_Badge_Weight";
    public const string LuckyWheel_Badge_Count = "LuckyWheel_Badge_Count";

    public const string LuckyWheel_Col_Color = "LuckyWheel_Col_Color";
    public const string LuckyWheel_Col_Name = "LuckyWheel_Col_Name";
    public const string LuckyWheel_Col_Weight = "LuckyWheel_Col_Weight";
    public const string LuckyWheel_Col_Odds = "LuckyWheel_Col_Odds";
    public const string LuckyWheel_Col_Toggle = "LuckyWheel_Col_Toggle";
    public const string LuckyWheel_Col_Delete = "LuckyWheel_Col_Delete";
    public const string LuckyWheel_Stepper_Decrease = "LuckyWheel_Stepper_Decrease";
    public const string LuckyWheel_Stepper_Increase = "LuckyWheel_Stepper_Increase";
    public const string LuckyWheel_Stepper_Tip = "LuckyWheel_Stepper_Tip";

    public const string LuckyWheel_Empty_Title = "LuckyWheel_Empty_Title";
    public const string LuckyWheel_Empty_Subtitle = "LuckyWheel_Empty_Subtitle";

    public const string LuckyWheel_SpinBtn = "LuckyWheel_SpinBtn";
    public const string LuckyWheel_StopBtn = "LuckyWheel_StopBtn";
    public const string LuckyWheel_Duration = "LuckyWheel_Duration";
    public const string LuckyWheel_Physics = "LuckyWheel_Physics";
    public const string LuckyWheel_Sound = "LuckyWheel_Sound";
    public const string LuckyWheel_Confetti = "LuckyWheel_Confetti";
    public const string LuckyWheel_HistoryBtn = "LuckyWheel_HistoryBtn";

    public const string LuckyWheel_History_Title = "LuckyWheel_History_Title";
    public const string LuckyWheel_History_Clear = "LuckyWheel_History_Clear";
    public const string LuckyWheel_History_CopyAll = "LuckyWheel_History_CopyAll";
    public const string LuckyWheel_History_Empty = "LuckyWheel_History_Empty";

    public const string LuckyWheel_Winner_Title = "LuckyWheel_Winner_Title";
    public const string LuckyWheel_Winner_SpinAgain = "LuckyWheel_Winner_SpinAgain";
    public const string LuckyWheel_Winner_RemoveAndSpin = "LuckyWheel_Winner_RemoveAndSpin";
    public const string LuckyWheel_Winner_RemoveTip = "LuckyWheel_Winner_RemoveTip";
    public const string LuckyWheel_Winner_CopyName = "LuckyWheel_Winner_CopyName";
    public const string LuckyWheel_Winner_Close = "LuckyWheel_Winner_Close";

    public const string LuckyWheel_Import_Title = "LuckyWheel_Import_Title";
    public const string LuckyWheel_Import_Subtitle = "LuckyWheel_Import_Subtitle";
    public const string LuckyWheel_Import_TabFile = "LuckyWheel_Import_TabFile";
    public const string LuckyWheel_Import_NoFile = "LuckyWheel_Import_NoFile";
    public const string LuckyWheel_Import_ChooseFile = "LuckyWheel_Import_ChooseFile";
    public const string LuckyWheel_Import_TabPaste = "LuckyWheel_Import_TabPaste";
    public const string LuckyWheel_Import_PasteTip = "LuckyWheel_Import_PasteTip";
    public const string LuckyWheel_Import_Mode = "LuckyWheel_Import_Mode";
    public const string LuckyWheel_Import_Replace = "LuckyWheel_Import_Replace";
    public const string LuckyWheel_Import_Append = "LuckyWheel_Import_Append";
    public const string LuckyWheel_Import_Cancel = "LuckyWheel_Import_Cancel";
    public const string LuckyWheel_Import_Confirm = "LuckyWheel_Import_Confirm";

    // Other Feature Modules
    public const string BatchRename_Title = "BatchRename_Title";
    public const string BatchRename_Subtitle = "BatchRename_Subtitle";
    public const string BatchFolder_Title = "BatchFolder_Title";
    public const string BatchFolder_Subtitle = "BatchFolder_Subtitle";
    public const string Checksum_Title = "Checksum_Title";
    public const string Checksum_Subtitle = "Checksum_Subtitle";
    public const string FileTools_Title = "FileTools_Title";
    public const string FileTools_Subtitle = "FileTools_Subtitle";
    public const string TextInspector_Title = "TextInspector_Title";
    public const string TextInspector_Subtitle = "TextInspector_Subtitle";
}


#include "FilenameLists.h"

FileNameList g_lstEncTxtFileNames;

void InitializeEncryptedFiles()
{
    g_lstEncTxtFileNames.push_back("DIVISIONINFO.TXT");
    g_lstEncTxtFileNames.push_back("divisioninfo.txt");
    g_lstEncTxtFileNames.push_back("GATEPORT.TXT");
    g_lstEncTxtFileNames.push_back("gateport.txt");

    // Item Data Files
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_5000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_10000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_15000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_20000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_25000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_30000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_35000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_40000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_45000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemdata_50000.txt");

    // Character Data Files
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_5000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_10000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_15000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_20000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_25000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_30000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_35000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_40000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_45000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\characterdata_50000.txt");

    // Skill Data Files
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_5000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_10000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_15000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_20000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_25000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_30000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_35000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_40000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_45000.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_50000.txt");

    // Encrypted Skill Data Files
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldataenc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_5000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_10000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_15000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_20000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_25000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_30000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_35000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_40000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_45000enc.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_50000enc.txt");

    // Game Configuration and Data Files

    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\aaa.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\abusefilter.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\actionwnddata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\atstructeffect.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\collectionbook_item.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\collectionbook_theme.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\dg.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\effectenvsnd.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\effectsound.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\erasablemastery.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\erasableskill.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\eventdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\eventguidedata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\eventword.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\eventzonedata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\fmncategorytreedata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\fmntidgroupmapdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\force_addobject.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\gachaitemset.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\gachanpcmap.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\gameguidedata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\gameworldconfigdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\gameworlddata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\groupworld_config_forclient.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\hwanleveldata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\item_grouping.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\itemeffect.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\learnablemastery.txt");

    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\learnableskill.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\leveldata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\levelgold.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\magicoption.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\magicoptionassign.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\mallitemmenulistdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\maxtradescaledata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\messagetipdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\npcchat.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\npcpos.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\optimize_clothes.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\questcontentsdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\questdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refabilitybyitemoptleveldata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refaccesspermissionofshop.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refconditiontobuyscrapitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refconditiontosellpackageitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refconditiontosellscrapitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refeventreward.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refeventrewarditems.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refgachatreeforclientuidata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refmagicoptbyitemoptleveldata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refmagicoptgroup.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refmappingshopgroup.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refmappingshopwithtab.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refoptionalteleport.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refpackageitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refpricepolicyofitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refquestrewarditems.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refqusetreward.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refregion.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refrentitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refrewardpolicytobuyscrapitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refrewardpolicytosellpackageitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refrewardpolicytosellscrapitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refscrapofpackageitem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refservereventid.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refsetitemgroup.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refshop.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refshopgoods.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refshopgroup.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refshopitemstockperiod.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refshoptab.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refshoptabgroup.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refsiegeblessbuff.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refsiegedungeon.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\refskillbyitemoptleveldata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\reftreatitemofshop.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\regioncode.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\regioninfo.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\shopdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\shopgroupdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\shopitemdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\shopitemstockquantity.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\shoptabdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\siegefortress.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\siegefortressbattlerank.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\siegefortressguard.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\siegefortressitemforge.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\siegefortressreward.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\siegestructupgradedata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilldata_virtual.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skilleffect.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skillgroup.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\skillmasterydata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\specialnpcdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\teleportbuilding.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\teleportdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\teleportlink.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\testsml.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textdata_equip&skill.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textdata_object.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textdataname.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textevent.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\texthelp.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textquest.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textquest_otherstring.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textquest_speech&name.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textquest_queststring.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\texttooltipdata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textuisystem.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\textzonename.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\usableresobjiddata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\wlocalmap.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\worldmap_instanceinfo.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\worldmap_localinfo.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\worldmap_mapinfo.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\worldmapguidedata.txt");
    g_lstEncTxtFileNames.push_back("server_dep\\silkroad\\textdata\\worldmapguidedata_region.txt");

}
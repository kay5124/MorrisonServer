using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MorrisonServer.Models.api_Delivery
{
    public class MD_T10_driver
    {
        public object sysUserId { get; set; }
        public object carId { get; set; }
        public object trailerId { get; set; }
        public object dcId { get; set; }
        public object tranCompId { get; set; }
        public object ticket { get; set; }
        public object clientIp { get; set; }
    }

    public class MD_T30_carshdu
    {
        public string RowStatus { get; set; }
        public object appSysId { get; set; }
        public object latitudeDev { get; set; }
        public object longitudeDev { get; set; }
        public object dcShip { get; set; }
        public object dispatch_id { get; set; }
        public object dcId { get; set; }
        public object delDate { get; set; }
        public object pickupTime { get; set; }
        public object tranMode { get; set; }
        public object carType { get; set; }
        public object arrangeType { get; set; }
        public object arrangeNum { get; set; }
        public object detailCount { get; set; }
        public object shipPoints { get; set; }
        public object driverCnt { get; set; }
        public object tranCompId { get; set; }
        public object carId { get; set; }
        public object trailerId { get; set; }
        public object driverId { get; set; }
        public object contactTel { get; set; }
        public object tranCompId2 { get; set; }
        public object carId2 { get; set; }
        public object trailerId2 { get; set; }
        public object driverId2 { get; set; }
        public object contactTel2 { get; set; }
        public object realTimeOut { get; set; }
        public object realTimeIn { get; set; }
        public object realKMOut { get; set; }
        public object realKMIn { get; set; }
        public object realShipTimeScale { get; set; }
        public object realKMTotal { get; set; }
        public object plts { get; set; }
        public object sqr { get; set; }
        public object wgt { get; set; }
        public object totalBoxesQty { get; set; }
        public object shipFlowStatus { get; set; }
        public object realDelDate { get; set; }
        public object realFinishTime { get; set; }
        public object sysUserId { get; set; }
        public object bindTime { get; set; }
        public object bindEndTime { get; set; }
        public object sysUserId2 { get; set; }
        public object bindTime2 { get; set; }
        public object sysUserId_run { get; set; }
        public object driverSeq { get; set; }
        public object taskStatus { get; set; }
        public object flagTrans { get; set; }
        public object longitudeCar { get; set; }
        public object latitudeCar { get; set; }
        public object memo { get; set; }
        public object cfs_timezone { get; set; }
        public object creatToPodTime { get; set; }
        public object ediToPodTime { get; set; }
        public object podToEdiTime { get; set; }
        public object creatSys { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }
        public object ticket { get; set; }
        public object clientIp { get; set; }
    }

    public class MD_T30_carinv
    {

        public string RowStatus { get; set; }
        public object appSysId { get; set; }
        public object longitudeDev { get; set; }
        public object latitudeDev { get; set; }
        public object dcShip { get; set; }
        public object bol_no { get; set; }
        public object arriveSeq { get; set; }
        public object dcOrder { get; set; }
        public object owner { get; set; }
        public object ownerDocNo { get; set; }
        public object shipName { get; set; }
        public object shipName2 { get; set; }
        public object shipAddr { get; set; }
        public object shipAddr2 { get; set; }
        public object shipCity { get; set; }
        public object shipState { get; set; }
        public object shipZip { get; set; }
        public object countryId { get; set; }
        public object contactPerson { get; set; }
        public object contactTel { get; set; }
        public object ship_instruction { get; set; }
        public object plts { get; set; }
        public object sqr { get; set; }
        public object wgt { get; set; }
        public object totalBoxesQty { get; set; }
        public object realArriveTime { get; set; }
        public object realServiceStartTime { get; set; }
        public object realLeaveTime { get; set; }
        public object realDistance { get; set; }
        public object realTimeScale { get; set; }
        public object realArriveKm { get; set; }
        public object realArriveTemp { get; set; }
        public object statusx1 { get; set; }
        public object arriveSeqD { get; set; }
        public object schddeliv_d { get; set; }
        public object schddeliv_t_from { get; set; }
        public object schddeliv_t_to { get; set; }
        public object estiArriveTime { get; set; }
        public object estiTimeScale { get; set; }
        public object estiArriveKm { get; set; }
        public object estiArriveTemp { get; set; }
        public object realArriveTime2 { get; set; }
        public object realLeaveTime2 { get; set; }
        public object statusId2 { get; set; }
        public object POD_name { get; set; }
        public object FileId_02 { get; set; }
        public object taskStatus { get; set; }
        public object statusType3 { get; set; }
        public object statusId3 { get; set; }
        public object memo { get; set; }
        public object sensitech_no { get; set; }
        public object security_level { get; set; }
        public object escort { get; set; }
        public object shipto_timezone { get; set; }
        public object creatToPodTime { get; set; }
        public object ediToPodTime { get; set; }
        public object podToEdiTime { get; set; }
        public object flagTrans { get; set; }
        public object creatSys { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }
        public object type { get; set; }
        public object base64img { get; set; }
        public object fileType { get; set; }
        public object ticket { get; set; }
        public object clientIp { get; set; }
    }

    public class MD_T30_carinv_arriveSeq
    {
        public object dispatch_id { get; set; }
        public List<shipOrder> newOrderArr { get; set; }
        public object ticket { get; set; }
        public object clientIp { get; set; }
    }

    public class shipOrder
    {
        public object arriveSeqD { get; set; }
        public object bol_no { get; set; }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MorrisonServer.Models.Base
{

    public class MD_T30_carshdu
    {
        public object dispatch_id { get; set; }
        public object tranCompId { get; set; }
        public object carId { get; set; }
        public object trailerId { get; set; }
        public object driverId { get; set; }
        public object driverId2 { get; set; }
    }

    public class MD_Login
    {
        public object userName { get; set; }
        public object userId { get; set; }
        public object password { get; set; }
        public object email { get; set; }
        public object contactTel { get; set; }
        public object zip { get; set; }
        public object addr { get; set; }
    }
    public class MD_S10_group
    {
        public object RowStatus { get; set; }
        public object groupId { get; set; }
        public object groupName { get; set; }
        public object statusId { get; set; }
        public object statusType { get; set; }
        public object statusName { get; set; }
        public object memo { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }
    }

    public class MD_S10_funcLimit
    {
        public object RowStatus { get; set; }
        public object realRow { get; set; }
        public object funcId { get; set; }
        public object groupId { get; set; }
        public object limitId { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }

    }

    public class MD_S10_users
    {
        public object RowStatus { get; set; }
        public object appSysId { get; set; }
        public object sysUserId { get; set; }
        public object userName { get; set; }
        public object userId { get; set; }
        public object password { get; set; }
        public object email { get; set; }
        public object contactTel { get; set; }
        public object contactTel2 { get; set; }
        public object statusId { get; set; }
        public object deviceLimit { get; set; }
        public object groups { get; set; }
        public object groupsName { get; set; }
        public object chkUnSavePW { get; set; }
        public object memo { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }
        public object platform { get; set; }
        public object model { get; set; }
        public object UUID { get; set; }
        public object appVer { get; set; }
        public object ticket { get; set; }
    }

    public class MD_S00_systemConfig
    {
        public object RowStatus { get; set; }
        //public object systemId { get; set; }
        public object appSysId { get; set; }
        public object systemName { get; set; }
        public object version { get; set; }
        public object platform { get; set; }
        public object statusId { get; set; }
        public object statusName { get; set; }
        public object statusId2 { get; set; }
        public object statusName2 { get; set; }
        public object aUrl { get; set; }
        public object iUrl { get; set; }
        public object ticket { get; set; }
    }

    public class MD_S10_regLog
    {
        public object RowStatus { get; set; }

        public object carId { get; set; }
        public object trailerId { get; set; }
        public object dcId { get; set; }
        public object tranCompId { get; set; }
        public object rowId { get; set; }
        public object appSysId { get; set; }
        public object userId { get; set; }
        public object password { get; set; }
        public object userName { get; set; }
        public object contactTel { get; set; }
        public object contactTel2 { get; set; }
        public object email { get; set; }
        public object compName { get; set; }
        public object userTitle { get; set; }
        public object UUID { get; set; }
        public object model { get; set; }
        public object platform { get; set; }
        public object statusId { get; set; }
        public object statusId2 { get; set; }
        public object verifyCode { get; set; }
        public object verifyCreatTime { get; set; }
        public object memo { get; set; }
        public object creatTime { get; set; }
        public object sysUserId { get; set; }
        public object ticket { get; set; }
        public object clientIp { get; set; }
    }

    public class MD_S10_forgetLog
    {
        public object rowId { get; set; }
        public object userId { get; set; }
        public object password { get; set; }
        public object email { get; set; }
        public object sysUserId { get; set; }
        public object verifyCode { get; set; }
        public object verifyCreatTime { get; set; }
        public object creatTime { get; set; }
        public object ticket { get; set; }
    }

    public class MD_App_userDevice
    {
        public object RowStatus { get; set; }
        public object sysUserId { get; set; }
        public object UUID { get; set; }
        public object isUse { get; set; }
        public object isNotice { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }
    }

    public class MD_App_group
    {
        public object RowStatus { get; set; }
        public object appGroupId { get; set; }
        public object appGroupName { get; set; }
        public object statusId { get; set; }
        public object statusType { get; set; }
        public object statusName { get; set; }
        public object memo { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }
        public object chkAll { get; set; }
        public object chk { get; set; }
    }

    public class MD_App_noti
    {
        public object RowStatus { get; set; }
        public object notiId { get; set; }
        public object title { get; set; }
        public object msg { get; set; }
        public object msgHtml { get; set; }
        public object pushTime { get; set; }
        public object statusType { get; set; }
        public object statusId { get; set; }
        public object isPushAll { get; set; }
        public object soundId { get; set; }
        public object param { get; set; }
        public object backcolor { get; set; }
        public object appGroups { get; set; }
        public object appGroupNames { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }
    }

    public class MD_T10_dcTranComp
    {

        public string RowStatus { get; set; }
        public object dcId { get; set; }
        public object tranCompId { get; set; }
        public object memo { get; set; }
        public object statusType { get; set; }
        public object statusId { get; set; }
        public object creatUser { get; set; }
        public object creatTime { get; set; }
        public object actUser { get; set; }
        public object actTime { get; set; }

    }

    public class MD_A20040
    {
        public string RowStatus { get; set; }
        public object sysUserId { get; set; }
        public object dcId { get; set; }
        public object tranCompId { get; set; }
        public object countryCode { get; set; }
        public object userName { get; set; }
        public object userId { get; set; }
        public object password { get; set; }
        public object email { get; set; }
        public object license { get; set; }
        public object expDate { get; set; }
        public object statusId { get; set; }
        public object memo { get; set; }
        public object contactTel { get; set; }
        public object contactTel2 { get; set; }

    }

}
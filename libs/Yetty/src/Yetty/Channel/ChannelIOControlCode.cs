namespace Com.Cybersoft4u.Yetty.Channel
{
    public enum ChannelIOControlCode
    {
        /// <summary>
        ///     這個值等於 Winsock 2 SIO_ABSORB_RTRALERT 常數。
        /// </summary>
        AbsorbRouterAlert,

        /// <summary>
        ///     使用其索引所識別的介面聯結多點傳送群組。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_INDEX_ADD_MCAST 常數。
        /// </summary>
        AddMulticastGroupOnInterface,

        /// <summary>
        ///     啟用通訊端通訊協定家族之本機介面清單何時變更的告知接收。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_ADDRESS_LIST_CHANGE 常數。
        /// </summary>
        AddressListChange,

        /// <summary>
        ///     傳回通訊端可繫結至的本機介面清單。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_ADDRESS_LIST_QUERY 常數。
        /// </summary>
        AddressListQuery,

        /// <summary>
        ///     排序 AddressListQuery 欄位所傳回的結構，並加入 IPv6 位址的領域 ID 資訊。Windows XP (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2
        ///     SIO_ADDRESS_LIST_SORT 常數。
        /// </summary>
        AddressListSort,

        /// <summary>
        ///     使這個通訊端與隨附介面的指定控制碼產生關聯。請參閱 Winsock 2 參考中的適當通訊協定特定附錄或特定隨附介面的文件，以瞭解詳細資料。建議您使用元件物件模型 (Component Object
        ///     Model，COM)，而不要使用這個 IOCTL，探索及追蹤通訊端可能支援的其他介面。使用這個控制碼，可提供與 COM 不可用或由於其他原而因無法使用之系統的回溯相容性。這個值等於 Winsock 2
        ///     SIO_ASSOCIATE_HANDLE 常數。
        /// </summary>
        AssociateHandle,

        /// <summary>
        ///     啟用資料何時等待接收的告知。這個值等於 Winsock 2 FIOASYNC 常數。
        /// </summary>
        AsyncIO,

        /// <summary>
        ///     將通訊端繫結至指定的介面索引。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_INDEX_BIND 常數。
        /// </summary>
        BindToInterface,

        /// <summary>
        ///     傳回可供讀取的位元組數目。這個值等於 Winsock 2 FIONREAD 常數。
        /// </summary>
        DataToRead,

        /// <summary>
        ///     移除 多點傳送群組中的通訊端。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_INDEX_ADD_MCAST 常數。
        /// </summary>
        DeleteMulticastGroupFromInterface,

        /// <summary>
        ///     內送訊息佇列已滿時，以內送資料包 (Datagram) 取代最先加入佇列的資料包。這個值等於 Winsock 2 SIO_ENABLE_CIRCULAR_QUEUEING 常數。
        /// </summary>
        EnableCircularQueuing,

        /// <summary>
        ///     捨棄傳送佇列的內容。這個值等於 Winsock 2 SIO_FLUSH 常數。
        /// </summary>
        Flush,

        /// <summary>
        ///     傳回包含目前通訊端通訊協定家族 (Family) 之廣播位址的 SOCKADDR 結構。傳回的位址可與 SendTo 方法搭配使用。這個值等於 Winsock 2 SIO_GET_BROADCAST_ADDRESS
        ///     常數。這個值只可用於使用者資料包通訊協定 (User Datagram Protocol，UDP) 通訊端。
        /// </summary>
        GetBroadcastAddress,

        /// <summary>
        ///     取得非 Winsock 規格之一部分的提供者特定功能。功能是使用其提供者指派的 GUID 指定的。這個值等於 Winsock 2 SIO_GET_EXTENSION_FUNCTION_POINTER 常數。
        /// </summary>
        GetExtensionFunctionPointer,

        /// <summary>
        ///     傳回通訊端群組的服務品質 (QOS) 屬性。這個值已保留供日後使用，它等於 Winsock 2 SIO_GET_GROUP_QOS 常數。
        /// </summary>
        GetGroupQos,

        /// <summary>
        ///     擷取與通訊端相關聯的 QOS 結構。只在提供具有 QOS 能力傳輸的平台 (如 Windows Me、Windows 2000 (含) 以後版本) 上支援這個控制項。這個值等於 Winsock 2 SIO_GET_QOS 常數。
        /// </summary>
        GetQos,

        /// <summary>
        ///     控制傳送 TCP 保持活動的封包及其傳送間隔。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。如需其他資訊，請參閱 RFC 1122 第 4.2.3.6 章。這個值等於 Winsock 2
        ///     SIO_KEEPALIVE_VALS 常數。
        /// </summary>
        KeepAliveValues,

        /// <summary>
        ///     這個值等於 Winsock 2 SIO_LIMIT_BROADCASTS 常數。
        /// </summary>
        LimitBroadcasts,

        /// <summary>
        ///     設定用於輸出多點傳送封包的介面。介面由其索引識別。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_INDEX_MCASTIF 常數。
        /// </summary>
        MulticastInterface,

        /// <summary>
        ///     控制路由器可轉送多點傳送封包的次數，也稱為存留時間 (TTL) 或躍點計數。這個值等於 Winsock 2 SIO_MULTICAST_SCOPE 常數。
        /// </summary>
        MulticastScope,

        /// <summary>
        ///     控制通訊端傳送的多點傳送資料在通訊端接收佇列中是否顯示為內送資料。這個值等於 Winsock 2 SIO_MULTIPOINT_LOOPBACK 常數。
        /// </summary>
        MultipointLoopback,

        /// <summary>
        ///     控制命名空間查詢失效時通訊端是否接收到告知。Windows XP (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_NSP_NOTIFY_CHANGE 常數。
        /// </summary>
        NamespaceChange,

        /// <summary>
        ///     控制通訊端的封鎖行為。如果使用此控制碼指定的引數為零，則通訊端會處於封鎖模式。如果引數非零，則通訊端會處於未封鎖模式。這個值等於 Winsock  2 FIONBIO 常數。
        /// </summary>
        NonBlockingIO,

        /// <summary>
        ///     傳回等待接收之 Out-of-Band Data 的相關資訊。在資料流通訊端上使用此控制碼時，傳回值表示可用的位元組數目。
        /// </summary>
        OobDataRead,

        /// <summary>
        ///     擷取基礎提供者的 SOCKET 控制代碼。這個控制代碼可用於接收隨插即用事件告知。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2
        ///     SIO_QUERY_TARGET_PNP_HANDLE 常數。
        /// </summary>
        QueryTargetPnpHandle,

        /// <summary>
        ///     啟用網路上所有 IPv4 封包的接收。通訊端必須擁有通訊協定家族 InterNetwork，通訊端類型必須為 Raw，且通訊協定類型必須為
        ///     IP。目前使用者必須屬於本機電腦上的系統管理員群組，且通訊端必須繫結至特定的連接埠。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_RCVALL 常數。
        /// </summary>
        ReceiveAll,

        /// <summary>
        ///     啟用網路上所有網際網路群組管理通訊協定 (IGMP) 封包的接收。通訊端必須擁有通訊協定家族 InterNetwork，通訊端類型必須為 Raw，且通訊協定類型必須為
        ///     Igmp。目前使用者必須屬於本機電腦上的系統管理員群組，且通訊端必須繫結至特定的連接埠。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2
        ///     SIO_RCVALL_IGMPMCAST 常數。
        /// </summary>
        ReceiveAllIgmpMulticast,

        /// <summary>
        ///     啟用網路上所有多點傳送 IPv4 封包的接收。這些封包的目的位址介於 224.0.0.0 到 239.255.255.255。通訊端必須擁有通訊協定家族 InterNetwork，通訊端類型必須為 Raw，且通訊協定類型必須為
        ///     Udp。目前使用者必須屬於本機電腦上的系統管理員群組，且通訊端必須繫結至特定的連接埠。Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_RCVALL_MCAST 常數。
        /// </summary>
        ReceiveAllMulticast,

        /// <summary>
        ///     啟用本機介面用於存取遠端端點變更時的告知接收。這個值等於 Winsock 2 SIO_ROUTING_INTERFACE_CHANGE 常數。
        /// </summary>
        RoutingInterfaceChange,

        /// <summary>
        ///     傳回可用於連接指定遠端位址的介面位址。這個值等於 Winsock 2 SIO_ROUTING_INTERFACE_QUERY 常數。
        /// </summary>
        RoutingInterfaceQuery,

        /// <summary>
        ///     設定通訊端群組的服務品質 (QOS) 屬性。這個值已保留供日後使用，它等於 Winsock 2 SIO_SET_GROUP_QOS 常數。
        /// </summary>
        SetGroupQos,

        /// <summary>
        ///     設定通訊端的服務品質 (QOS) 屬性。QOS 定義通訊端的頻寬需求。Windows Me、Windows 2000 (含) 以後版本的作業系統可以支援這個控制碼。這個值等於 Winsock 2 SIO_SET_QOS 常數。
        /// </summary>
        SetQos,

        /// <summary>
        ///     傳回隨附介面內容中有效的通訊端控制代碼。這個值等於 Winsock 2 SIO_TRANSLATE_HANDLE 常數。
        /// </summary>
        TranslateHandle,

        /// <summary>
        ///     設定用於輸出單點傳送封包的介面。這個值等於 Winsock 2 SIO_UCAST_IF 常數。
        /// </summary>
        UnicastInterface
    }
}

namespace Com.Cybersoft4u.Yetty.Channel
{
    public enum ChannelOptionName
    {
        /// <summary>
        ///     通訊端正在接聽。
        /// </summary>
        AcceptConnection,

        /// <summary>
        ///     加入 IP 群組成員資格。
        /// </summary>
        AddMembership,

        /// <summary>
        ///     聯結來源群組。
        /// </summary>
        AddSourceMembership,

        /// <summary>
        ///     封鎖來自來源的資料。
        /// </summary>
        BlockSource,

        /// <summary>
        ///     允許在通訊端上傳送廣播訊息。
        /// </summary>
        Broadcast,

        /// <summary>
        ///     使用 RFC-1222 中定義的緊急資料。這個選項只能設定一次，一旦設定就無法關閉。
        /// </summary>
        BsdUrgent,

        /// <summary>
        ///     設定或取得 UDP 總和檢查碼涵蓋範圍。
        /// </summary>
        ChecksumCoverage,

        /// <summary>
        ///     記錄偵錯資訊。
        /// </summary>
        Debug,

        /// <summary>
        ///     不要將 IP 資料包分成片段。
        /// </summary>
        DontFragment,

        /// <summary>
        ///     立即關閉通訊端。
        /// </summary>
        DontLinger,

        /// <summary>
        ///     不進行路由，直接將封包傳送至介面位址。
        /// </summary>
        DontRoute,

        /// <summary>
        ///     捨棄 IP 群組成員資格。
        /// </summary>
        DropMembership,

        /// <summary>
        ///     捨棄來源群組。
        /// </summary>
        DropSourceMembership,

        /// <summary>
        ///     取得錯誤狀態並清除。
        /// </summary>
        Error,

        /// <summary>
        ///     啟用將繫結至獨佔存取的通訊端。
        /// </summary>
        ExclusiveAddressUse,

        /// <summary>
        ///     使用 RFC-1222 中定義的已傳輸資料。這個選項只能設定一次，一旦設定就無法關閉。
        /// </summary>
        Expedited,

        /// <summary>
        ///     表示應用程式將提供輸出資料包的 IP 標頭。
        /// </summary>
        HeaderIncluded,

        /// <summary>
        ///     指定網際網路通訊協定第 6 版 (IPv6) 封包的最大路由器躍點數。這與網際網路通訊協定第 4 版的存留時間 (TTL) 類似。
        /// </summary>
        HopLimit,

        /// <summary>
        ///     指定要插入輸出資料包的 IP 選項。
        /// </summary>
        IPOptions,

        /// <summary>
        ///     讓 IPv6 通訊端限制在指定範圍的值，例如相同連結本機或網站本機之前置詞的位址。這個通訊端選項可以讓應用程式對 IPv6 通訊端設定存取限制。這類限制可以讓應用程式在私人 LAN
        ///     上執行，簡便又穩當地強化應用程式對外部攻擊的抵禦。這個通訊端選項可放大或縮小接聽通訊端的範圍，根據需要啟用公用和私用使用者的無限制存取，或視需要限制成僅可存取相同的站台。這個通訊端選項已定義 IPProtectionLevel
        ///     列舉中所指定的保護層級。
        /// </summary>
        IPProtectionLevel,

        /// <summary>
        ///     設定 IP 標頭的存留時間欄位。
        /// </summary>
        IpTimeToLive,

        /// <summary>
        ///     指出針對 AF_INET6 位址家族建立的通訊端是否限制僅供 IPv6 通訊使用。針對 AF_INET6 位址家族建立的通訊端，可同時提供 IPv6 及 IPv4 通訊使用。某些應用程式可能希望將使用限制在針對 AF_INET6
        ///     位址家族所建立的通訊端，以及僅提供 IPv6 通訊使用。如果這個值不是零 (Windows 上的預設值)，則會使用針對 AF_INET6 位址家族建立的通訊端，並且只用來傳送和接收 IPv6 封包。如果這個值為零，則針對
        ///     AF_INET6 位址家族建立的通訊端，可用來傳送和接收往返 IPv6 或 IPv4 位址的封包。請注意，必須使用 IPv4 對應位址，才能發揮與 IPv4 位址互動的能力。Windows Vista (含)
        ///     以後版本支援這個通訊端選項。
        /// </summary>
        IPv6Only,

        /// <summary>
        ///     使用 Keep-Alive。
        /// </summary>
        KeepAlive,

        /// <summary>
        ///     如果有未傳送的資料，則延遲關閉。
        /// </summary>
        Linger,

        /// <summary>
        ///     不支援，如果使用則會擲回 SocketException。
        /// </summary>
        MaxConnections,

        /// <summary>
        ///     設定用於輸出多點傳送封包的介面。
        /// </summary>
        MulticastInterface,

        /// <summary>
        ///     IP 多點傳送回送。
        /// </summary>
        MulticastLoopback,

        /// <summary>
        ///     IP 多點傳送存留時間。
        /// </summary>
        MulticastTimeToLive,

        /// <summary>
        ///     在總和檢查碼設定為零的情況下傳送 UDP 資料包。
        /// </summary>
        NoChecksum,

        /// <summary>
        ///     停用用於傳送聯合的 Nagle 演算法。
        /// </summary>
        NoDelay,

        /// <summary>
        ///     在一般資料流中接收頻外 (Out-of-Band) 資料。
        /// </summary>
        OutOfBandInline,

        /// <summary>
        ///     傳回所接收封包的相關資訊。
        /// </summary>
        PacketInformation,

        /// <summary>
        ///     指定保留給接收的每一通訊端緩衝區總空間。這與最大訊息大小或 TCP 視窗大小無關。
        /// </summary>
        ReceiveBuffer,

        /// <summary>
        ///     指定 Receive 作業的下限標準。
        /// </summary>
        ReceiveLowWater,

        /// <summary>
        ///     接收逾時。這個選項只適用於同步方法，它對 BeginSend 方法之類的非同步方法沒有作用。
        /// </summary>
        ReceiveTimeout,

        /// <summary>
        ///     允許要繫結至已使用中位址的通訊端。
        /// </summary>
        ReuseAddress,

        /// <summary>
        ///     指定保留給傳送的每一通訊端緩衝區總空間。這與最大訊息大小或 TCP 視窗大小無關。
        /// </summary>
        SendBuffer,

        /// <summary>
        ///     指定指定 Send 作業的下限標準。
        /// </summary>
        SendLowWater,

        /// <summary>
        ///     傳送逾時。這個選項只適用於同步方法，它對 BeginSend 方法之類的非同步方法沒有作用。
        /// </summary>
        SendTimeout,

        /// <summary>
        ///     取得通訊端類型。
        /// </summary>
        Type,

        /// <summary>
        ///     變更服務欄位的 IP 標頭類型。
        /// </summary>
        TypeOfService,

        /// <summary>
        ///     取消封鎖先前所封鎖的來源。
        /// </summary>
        UnblockSource,

        /// <summary>
        ///     藉由使用現有通訊端的屬性，更新已接受之通訊端的屬性。這相當於使用 Winsock2 SO_UPDATE_ACCEPT_CONTEXT 通訊端選項，而且只有連線導向的通訊端才支援。
        /// </summary>
        UpdateAcceptContext,

        /// <summary>
        ///     藉由使用現有通訊端的屬性，更新已連接之通訊端的屬性。這相當於使用 Winsock2 SO_UPDATE_CONNECT_CONTEXT 通訊端選項，而且只有連線導向的通訊端才支援。
        /// </summary>
        UpdateConnectContext,

        /// <summary>
        ///     盡可能不使用硬體。
        /// </summary>
        UseLoopback,

        /// <summary>
        /// Backlog of tcp server socket
        /// </summary>
        Backlog
    }
}

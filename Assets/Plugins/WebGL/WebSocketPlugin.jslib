mergeInto(LibraryManager.library, {
WebSocketConnect: function(url) {
        var urlStr = UTF8ToString(url);

        if (window.gameWebSocket && window.gameWebSocket.readyState === WebSocket.OPEN)
        {
            console.log('[WebSocket] Already connected');
            return;
        }

        window.gameWebSocket = new WebSocket(urlStr);

        window.gameWebSocket.onopen = function() {
            console.log('[WebSocket] 연결 성공');
            SendMessage('WebSocketManager', 'OnWebSocketOpen', '');
        }
        ;

        window.gameWebSocket.onerror = function(error) {
            console.error('[WebSocket] 에러:', error);
            SendMessage('WebSocketManager', 'OnWebSocketError', error.toString());
        }
        ;

        window.gameWebSocket.onmessage = function(event) {
            SendMessage('WebSocketManager', 'OnWebSocketMessage', event.data);
        }
        ;

        window.gameWebSocket.onclose = function(event) {
            console.log('[WebSocket] 연결 종료');
            SendMessage('WebSocketManager', 'OnWebSocketClose', '');
        }
        ;
    },
    
    WebSocketSend: function(message) {
        var msgStr = UTF8ToString(message);
        if (window.gameWebSocket && window.gameWebSocket.readyState === WebSocket.OPEN)
        {
            window.gameWebSocket.send(msgStr);
            console.log('[WS SEND]', msgStr);
        }
        else
        {
            console.error('[WebSocket] 연결 안됨');
        }
    },
    
    WebSocketClose: function() {
        if (window.gameWebSocket)
        {
            window.gameWebSocket.close();
        }
    },
    
    WebSocketIsConnected: function() {
        return window.gameWebSocket && window.gameWebSocket.readyState === WebSocket.OPEN ? 1 : 0;
    }
});
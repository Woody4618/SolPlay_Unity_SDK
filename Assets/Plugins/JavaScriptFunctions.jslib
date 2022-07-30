mergeInto(LibraryManager.library, {

   ConnectPhantom: async function () {
      if ('phantom' in window && window.phantom != null && window.phantom.solana != null) {
         try {
            const resp = await window.phantom.solana.connect();
            console.log(resp.publicKey.toString());
            window.unityInstance.SendMessage('JavaScriptWrapperService', 'OnPhantomConnected', resp.publicKey.toString());
         } catch (err) {
            window.alert(err);
         } 
      } else {
         window.alert("Please install phantom browser extension.");
      }
   }

});
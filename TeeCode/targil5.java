package targil5;

import com.intel.crypto.CryptoException;
import com.intel.crypto.HashAlg;
import com.intel.crypto.RsaAlg;
import com.intel.util.*;

//
// Implementation of DAL Trusted Application: targil5 
//
// **************************************************************************************************
// NOTE:  This default Trusted Application implementation is intended for DAL API Level 7 and above
// **************************************************************************************************

public class targil5 extends IntelApplet {

	/**
	 * This method will be called by the VM when a new session is opened to the Trusted Application 
	 * and this Trusted Application instance is being created to handle the new session.
	 * This method cannot provide response data and therefore calling
	 * setResponse or setResponseCode methods from it will throw a NullPointerException.
	 * 
	 * @param	request	the input data sent to the Trusted Application during session creation
	 * 
	 * @return	APPLET_SUCCESS if the operation was processed successfully, 
	 * 		any other error status code otherwise (note that all error codes will be
	 * 		treated similarly by the VM by sending "cancel" error code to the SW application).
	 */
	public int onInit(byte[] request) {
		DebugPrint.printString("Hello, DAL!");
		return APPLET_SUCCESS;
	}
	
	/**
	 * This method will be called by the VM to handle a command sent to this
	 * Trusted Application instance.
	 * 
	 * @param	commandId	the command ID (Trusted Application specific) 
	 * @param	request		the input data for this command 
	 * @return	the return value should not be used by the applet
	 */
	public int invokeCommand(int commandId, byte[] request) {
		
		DebugPrint.printString("Received command Id: " + commandId + ".");
		if(request != null)
		{
			DebugPrint.printString("Received buffer:");
			DebugPrint.printBuffer(request);
		}
		 byte[] myResponse = new byte[256];
		 byte[] myResponse2 = new byte[256];
		
		switch (commandId)
		{
		case 1:{
			try {
				
				storeToFlash(request, (short) 0);//save the crdit card in index 0
				FlashStorage.readFlashData(0, myResponse, 0);
				
				/*
				 * To return the response data to the command, call the setResponse
				 * method before returning from this method. 
				 * Note that calling this method more than once will 
				 * reset the response data previously set.
				 */
				setResponse(myResponse, 0, myResponse.length);
				
				break;
			}
			
			catch (Exception e) {
				String msg=e.getMessage();
			}
			}	
			
		case 2:
		{
			storeToFlash(request, (short)1);
		try {
		RsaAlg rsaAlg=RsaAlg.create();
	    rsaAlg.setPaddingScheme(RsaAlg.PAD_TYPE_PKCS1); 
	    rsaAlg.setKey(request, (short)0,(short) 256, request,(short) 256,(short) 4);
		byte[] card=new byte[256];
		int len=FlashStorage.readFlashData(0, card, 0);
		rsaAlg.encryptComplete(card, (short)0,(short) len ,myResponse2, (short)0);
		
		/*
		 * To return the response data to the command, call the setResponse
		 * method before returning from this method. 
		 * Note that calling this method more than once will 
		 * reset the response data previously set.
		 */
		setResponse(myResponse2, 0, myResponse2.length);
		break;	
		} 
		catch (Exception e) {
			String msg=e.getMessage();
		}
			
		}
			
		}
	
			
			
		
		

		/*
		 * In order to provide a return value for the command, which will be
		 * delivered to the SW application communicating with the Trusted Application,
		 * setResponseCode method should be called. 
		 * Note that calling this method more than once will reset the code previously set. 
		 * If not set, the default response code that will be returned to SW application is 0.
		 */
		setResponseCode(commandId);

		/*
		 * The return value of the invokeCommand method is not guaranteed to be
		 * delivered to the SW application, and therefore should not be used for
		 * this purpose. Trusted Application is expected to return APPLET_SUCCESS code 
		 * from this method and use the setResposeCode method instead.
		 */
		return APPLET_SUCCESS;
	}

	/**
	 * This method will be called by the VM when the session being handled by
	 * this Trusted Application instance is being closed 
	 * and this Trusted Application instance is about to be removed.
	 * This method cannot provide response data and therefore
	 * calling setResponse or setResponseCode methods from it will throw a NullPointerException.
	 * 
	 * @return APPLET_SUCCESS code (the status code is not used by the VM).
	 */
	public int onClose() {
		DebugPrint.printString("Goodbye, DAL!");
		return APPLET_SUCCESS;
	}
	
	int convertByteArrayToInt(byte[] byteArray) {
		if (byteArray == null || byteArray.length != 4)
			return 0;
		return (
				(0xff & byteArray[0]) << 24  |
				(0xff & byteArray[1]) << 16  |
				(0xff & byteArray[2]) << 8   |
				(0xff & byteArray[3]) << 0  
				);
	}
	
	byte[] intToBytes(final int num) {
		return new byte[] {
				(byte) ((num >> 24) & 0xff),
				(byte) ((num >> 16) & 0xff),
				(byte) ((num >> 8) & 0xff),
				(byte) ((num >> 0) & 0xff)
		};		
	}
	void storeToFlash(byte[] arr,short index) {
		
		FlashStorage.writeFlashData(index, arr, index, 200);
		
	}
	
}

//	byte [] seed=new byte [4];
//		FlashStorage.readFlashData(0, seed, 0);
//	short temp=(short)convertByteArrayToInt(seed);
//	HashAlg hashAlg = HashAlg.create(HashAlg.HASH_TYPE_SHA256);
	//byte [] password = new byte[2000];
	//hashAlg.processComplete(seed,(short) 0, (short)seed.length, password, (short)0);
    //myResponse=password;
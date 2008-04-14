// Audio.cpp : Defines the entry point for the application.

#include "stdafx.h"
#include <afxwin.h>
#include "Audio.h"
#include "CPropertyBag.h"
#include <windows.h>
#include <commctrl.h>
#include <streams.h>
#include <dmodshow.h>
#include <dmoreg.h>
#include <dmo.h>
#include <wmcodecids.h>
#include <dshow.h>
#include <strmif.h>
#include <evcode.h>
#include "Wininet.h"
#include <windows.h>
#include <commctrl.h>
#include <service.h>

#pragma comment(lib, "Wininet.lib")

#define MAX_LOADSTRING 100

// GDI Escapes for ExtEscape()
#define QUERYESCSUPPORT    8

// The following are unique to CE
#define GETVFRAMEPHYSICAL   6144
#define GETVFRAMELEN    6145
#define DBGDRIVERSTAT    6146
#define SETPOWERMANAGEMENT   6147
#define GETPOWERMANAGEMENT   6148

#define PHONE 

static const GUID CLSID_WavDest =
{ 0x3c78b8e2, 0x6c4d, 0x11d1, { 0xad, 0xe2, 0x0, 0x0, 0xf8, 0x75, 0x4b, 0x99 } };

static const GUID CLSID_Dump = {
	0x36a5f770, 0xfe4c, 0x11ce, { 0xa8, 0xed, 0x00, 0xaa, 0x00, 0x2f, 0xea, 0xb5} };

typedef enum _VIDEO_POWER_STATE {
	VideoPowerOn = 1,
	VideoPowerStandBy,
	VideoPowerSuspend,
	VideoPowerOff
} VIDEO_POWER_STATE, *PVIDEO_POWER_STATE;

typedef struct _VIDEO_POWER_MANAGEMENT {
	ULONG Length;
	ULONG DPMSVersion;
	ULONG PowerState;
} VIDEO_POWER_MANAGEMENT, *PVIDEO_POWER_MANAGEMENT;

// Forward declarations of functions included in this code module:
CComPtr<ICaptureGraphBuilder2>  pCaptureGraphBuilder;
CComPtr<IBaseFilter>            pVideoCap, pVideoEncoder, pAudioEncoder, pAudioCaptureFilter;
CComPtr<IBaseFilter>			pWavWriter;
CComPtr<IDMOWrapperFilter>      pVideoWrapperFilter, pAudioWrapperFilter;
CComPtr<IPersistPropertyBag>    pPropertyBag;
CComPtr<IGraphBuilder>          pGraph;
CComPtr<IMediaControl>          pMediaControl;
CComPtr<IMediaEvent>            pMediaEvent;
CComPtr<IMediaSeeking>          pMediaSeeking;
CComPtr<IFileSinkFilter2>       pFileSink;

CComPtr<IBaseFilter> pACMWrapper;  


VIDEOINFOHEADER *pVih;

LONGLONG dwEnd, dwStart =0;
long    lEventCode, lParam1, lParam2;
int count = 0;

bool encoding = false;

HRESULT hr = S_OK;

//CComVariant   varCamName;
//CPropertyBag  PropBag;	

//
//   FUNCTION: InitInstance(HINSTANCE, int)
//
//   PURPOSE: Saves instance handle and creates main window
//
//   COMMENTS:
//
//        In this function, we save the instance handle in a global variable and
//        create and display the main program window.
//
extern BOOL InitializeAudioRecording()
{
	CoInitialize( NULL );
	encoding = false;

	// Create the graph builder and the filtergraph
	pCaptureGraphBuilder.CoCreateInstance( CLSID_CaptureGraphBuilder );
	pGraph.CoCreateInstance( CLSID_FilterGraph );
	pCaptureGraphBuilder->SetFiltergraph( pGraph );

	// Query all the interfaces needed later
	pGraph.QueryInterface( &pMediaControl );
	pGraph.QueryInterface( &pMediaEvent );
	pGraph.QueryInterface( &pMediaSeeking );

	return TRUE;
}

extern BOOL PrepareAudioRecording(LPWSTR str){
	// Initialize the audio capture filter
	hr=pAudioCaptureFilter.CoCreateInstance( CLSID_AudioCapture );
	hr=pAudioCaptureFilter.QueryInterface( &pPropertyBag );
	hr=pPropertyBag->Load( NULL, NULL );
	hr=pGraph->AddFilter( pAudioCaptureFilter, L"Audio Capture Filter" );

	hr=pWavWriter.CoCreateInstance( CLSID_Dump );
	if (hr != S_OK)
		return FALSE;
	hr=pWavWriter->QueryInterface( IID_IFileSinkFilter, (void**) &pFileSink );

	hr=pFileSink->SetFileName( str, NULL );
	hr=pGraph->AddFilter( pWavWriter, L"Dump Writer" );

	// Connect the audio capture pin to the audio renderer. 
	hr=pCaptureGraphBuilder->RenderStream( &PIN_CATEGORY_CAPTURE,
										&MEDIATYPE_Audio, pAudioCaptureFilter,
										NULL, pWavWriter);

	if (hr != S_OK)
		return FALSE;

	// Block the capture.
	hr=pCaptureGraphBuilder->ControlStream( &PIN_CATEGORY_CAPTURE,
										&MEDIATYPE_Audio, pAudioCaptureFilter,
										 0, 0, 0, 0 );

	// Let's run the graph and wait one second before capturing. 
	hr=pMediaControl->Run();
	Sleep( 1000 );
	return TRUE;
}

extern BOOL StartAudioRecording(){
	dwEnd=MAXLONGLONG;
	OutputDebugString( L"Starting to capture the first file" );
	hr=pCaptureGraphBuilder->ControlStream( &PIN_CATEGORY_CAPTURE,
                                    &MEDIATYPE_Audio, pAudioCaptureFilter,
                                     &dwStart, &dwEnd, 0, 0 );
	if (hr = S_OK)
		return TRUE;

	return FALSE;
}

extern BOOL IsEncoding(){
	return encoding;
}

extern BOOL EndAudioRecording(){
	dwStart=0;
	dwEnd=MAXLONGLONG;
	OutputDebugString( L"Stopping the capture" );
	hr=pMediaSeeking->GetCurrentPosition( &dwEnd );
	hr=pCaptureGraphBuilder->ControlStream( &PIN_CATEGORY_CAPTURE,
                                    &MEDIATYPE_Audio, pAudioCaptureFilter,
                                     &dwStart, &dwEnd, 1, 2 );

	//hr=pMediaControl->StopWhenReady();
	//hr=pMediaEvent->WaitForCompletion(10000, EC_STREAM_CONTROL_STOPPED;

	// Wait for the ControlStream event. 

	//NOTE: waveCount is an attempt to gracefully exit the control structure
	//      if this fires without a properly functioning graph.
	//
	//      Do not use if the recorder device is connected to ANY middle
	//      filter that will be creating events (i.e. all of them)!!!  Removing 
	//      the need for this at hardware/driver level should be a top priority.

	int waveCount = 0;

	OutputDebugString( L"Wating for the control stream events" );
	do
	{
	    pMediaEvent->GetEvent( &lEventCode, &lParam1, &lParam2, INFINITE );
	    pMediaEvent->FreeEventParams( lEventCode, lParam1, lParam2 );

	    if( lEventCode == EC_STREAM_CONTROL_STOPPED ) {
			OutputDebugString( L"Received a control stream stop event" );
			count++;
		}
		waveCount++;
	} while( count < 1 || waveCount < 5000);

	pAudioCaptureFilter.Release();

	pAudioEncoder.Release();
	pMediaEvent.Release();
	pMediaSeeking.Release();
	pMediaControl.Release();
	pFileSink.Release();
	pWavWriter.Release();
	pAudioWrapperFilter.Release();
	pGraph.Release();
	pCaptureGraphBuilder.Release();
	pPropertyBag.Release();

	CoUninitialize();
	if (count == 0)
		return FALSE;

	OutputDebugString( L"The file has been captured" );
	
	return TRUE;
}



#ifdef DEBUG
int _tmain(){
	InitializeAudioRecording();
	PrepareAudioRecording(L"\\Storage Card\\filename.wav");
	StartAudioRecording();
	Sleep(40000);
	EndAudioRecording();
}
#endif


//-----  WIFI STUFFS

_GetWirelessDevices		pGetWirelessDevices = NULL;
_ChangeRadioState		pChangeRadioState = NULL;
_FreeDeviceList			pFreeDeviceList = NULL;

HINSTANCE	g_DllWrlspwr;

BOOL InitDLL()
{
	g_DllWrlspwr = LoadLibrary(TEXT("ossvcs.dll"));
	if (g_DllWrlspwr == NULL)
		return FALSE;
	pGetWirelessDevices   = (_GetWirelessDevices)GetProcAddress(g_DllWrlspwr,MAKEINTRESOURCE(GetWirelessDevice_ORDINAL));
	if (pGetWirelessDevices == NULL)
		return FALSE;
	
	pChangeRadioState   = (_ChangeRadioState)GetProcAddress(g_DllWrlspwr,MAKEINTRESOURCE(ChangeRadioState_ORDINAL));
	if (pChangeRadioState == NULL)
		return FALSE;
	
	pFreeDeviceList	   = (_FreeDeviceList)GetProcAddress(g_DllWrlspwr,MAKEINTRESOURCE(FreeDeviceList_ORDINAL));
	if (pFreeDeviceList == NULL)
		return FALSE;
	return TRUE;
}

//set the status of the desired wireless device
DWORD SetWDevState(DWORD dwDevice, DWORD dwState)
{
	RDD * pDevice = NULL;
    RDD * pTD;
    HRESULT hr;
	DWORD retval = 0;

//	InitDLL();
    hr = pGetWirelessDevices(&pDevice, 0);
	if(hr != S_OK) return -1;
    
    if (pDevice)
    {
        pTD = pDevice;

        // loop through the linked list of devices
        while (pTD)
        {
          if  (pTD->DeviceType == dwDevice)
          {
              hr = pChangeRadioState(pTD, dwState, RADIODEVICES_PRE_SAVE);
			  retval = 0;
          }
          
            pTD = pTD->pNext;
            
        }
        // Free the list of devices retrieved with    
        // GetWirelessDevices()
		pFreeDeviceList(pDevice);
    }

	if(hr == S_OK)return retval;
	
	return -2;
}

//get status of all wireless devices at once
DWORD GetWDevState(DWORD* bWifi, DWORD* bPhone, DWORD* bBT)
{
	RDD * pDevice = NULL;
    RDD * pTD;

    HRESULT hr;
	DWORD retval = 0;
	
    hr = pGetWirelessDevices(&pDevice, 0);

	if(hr != S_OK) return -1;
	
    if (pDevice)
    {
	    pTD = pDevice;

        // loop through the linked list of devices
		while (pTD)
		{
			switch (pTD->DeviceType)
			{
				case RADIODEVICES_MANAGED:
				*bWifi = pTD->dwState;
				break;
				case RADIODEVICES_PHONE:
				*bPhone = pTD->dwState;
				break;
				case RADIODEVICES_BLUETOOTH:
				*bBT = pTD->dwState;
				break;
				default:
				break;
			}
			pTD = pTD->pNext; 
	    }
        // Free the list of devices retrieved with    
        // GetWirelessDevices()
        pFreeDeviceList(pDevice);
    }

	if(hr == S_OK)return retval;
	
	return -2;
}

extern BOOL PrepareRadios(){
	return InitDLL();
}

extern BOOL CleanupRadios(){
	return FreeLibrary(g_DllWrlspwr);
}

extern int RadioStates(){
	DWORD	dwWifi, dwPhone, dwBT;
	GetWDevState(&dwWifi, &dwPhone, &dwBT);

	return(dwWifi + (dwPhone*2) + (dwBT*4));
}

extern BOOL EnableRadio(int devices, bool turnOn){
	if (devices%2 == 1){
		SetWDevState( RADIODEVICES_MANAGED, turnOn);
		devices--;
	}
	if (devices%4 == 2){
		SetWDevState( RADIODEVICES_PHONE, turnOn);
		devices--;
		devices--;
	}
	if (devices%8 == 4){
		SetWDevState( RADIODEVICES_BLUETOOTH, turnOn);
	}	
	return true;
}

int _tmain(int argc, _TCHAR* argv[])
{
	// Load ossvcs.dll
	InitDLL();

	DWORD	dwWifi, dwPhone, dwBT;
	GetWDevState(&dwWifi, &dwPhone, &dwBT);

	//start bluetooth
	SetWDevState( RADIODEVICES_BLUETOOTH, 1);
	//start phone
	SetWDevState( RADIODEVICES_PHONE, 1);
	//start WIFI
	SetWDevState( RADIODEVICES_MANAGED, 1);

	// Free ossvcs.dll
	FreeLibrary(g_DllWrlspwr);

	return 0;
}



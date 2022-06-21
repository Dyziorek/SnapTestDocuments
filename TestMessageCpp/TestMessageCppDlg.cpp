
// TestMessageCppDlg.cpp : implementation file
//

#include "pch.h"
#include "framework.h"
#include "TestMessageCpp.h"
#include "TestMessageCppDlg.h"
#include "afxdialogex.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif
#include <vector>
#include <string>
#include <sstream>


// CAboutDlg dialog used for App About

class CAboutDlg : public CDialogEx
{
public:
	CAboutDlg();

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_ABOUTBOX };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);    // DDX/DDV support

// Implementation
protected:
	DECLARE_MESSAGE_MAP()
};

CAboutDlg::CAboutDlg() : CDialogEx(IDD_ABOUTBOX)
{
}

void CAboutDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CAboutDlg, CDialogEx)
END_MESSAGE_MAP()


// CTestMessageCppDlg dialog



CTestMessageCppDlg::CTestMessageCppDlg(CWnd* pParent /*=nullptr*/)
	: CDialogEx(IDD_TESTMESSAGECPP_DIALOG, pParent)
{
	m_hIcon = AfxGetApp()->LoadIcon(IDR_MAINFRAME);
}

void CTestMessageCppDlg::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
}

BEGIN_MESSAGE_MAP(CTestMessageCppDlg, CDialogEx)
	ON_WM_SYSCOMMAND()
	ON_WM_PAINT()
	ON_WM_QUERYDRAGICON()
	ON_BN_CLICKED(IDC_BUTTON1, &CTestMessageCppDlg::OnBnClickedButton1)
	ON_BN_CLICKED(IDC_BUTTON2, &CTestMessageCppDlg::OnBnClickedButton2)
END_MESSAGE_MAP()


// CTestMessageCppDlg message handlers

BOOL CTestMessageCppDlg::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	// Add "About..." menu item to system menu.

	// IDM_ABOUTBOX must be in the system command range.
	ASSERT((IDM_ABOUTBOX & 0xFFF0) == IDM_ABOUTBOX);
	ASSERT(IDM_ABOUTBOX < 0xF000);

	CMenu* pSysMenu = GetSystemMenu(FALSE);
	if (pSysMenu != nullptr)
	{
		BOOL bNameValid;
		CString strAboutMenu;
		bNameValid = strAboutMenu.LoadString(IDS_ABOUTBOX);
		ASSERT(bNameValid);
		if (!strAboutMenu.IsEmpty())
		{
			pSysMenu->AppendMenu(MF_SEPARATOR);
			pSysMenu->AppendMenu(MF_STRING, IDM_ABOUTBOX, strAboutMenu);
		}
	}

	// Set the icon for this dialog.  The framework does this automatically
	//  when the application's main window is not a dialog
	SetIcon(m_hIcon, TRUE);			// Set big icon
	SetIcon(m_hIcon, FALSE);		// Set small icon

	// TODO: Add extra initialization here

	return TRUE;  // return TRUE  unless you set the focus to a control
}

void CTestMessageCppDlg::OnSysCommand(UINT nID, LPARAM lParam)
{
	if ((nID & 0xFFF0) == IDM_ABOUTBOX)
	{
		CAboutDlg dlgAbout;
		dlgAbout.DoModal();
	}
	else
	{
		CDialogEx::OnSysCommand(nID, lParam);
	}
}

// If you add a minimize button to your dialog, you will need the code below
//  to draw the icon.  For MFC applications using the document/view model,
//  this is automatically done for you by the framework.

void CTestMessageCppDlg::OnPaint()
{
	if (IsIconic())
	{
		CPaintDC dc(this); // device context for painting

		SendMessage(WM_ICONERASEBKGND, reinterpret_cast<WPARAM>(dc.GetSafeHdc()), 0);

		// Center icon in client rectangle
		int cxIcon = GetSystemMetrics(SM_CXICON);
		int cyIcon = GetSystemMetrics(SM_CYICON);
		CRect rect;
		GetClientRect(&rect);
		int x = (rect.Width() - cxIcon + 1) / 2;
		int y = (rect.Height() - cyIcon + 1) / 2;

		// Draw the icon
		dc.DrawIcon(x, y, m_hIcon);
	}
	else
	{
		CDialogEx::OnPaint();
	}
}

// The system calls this function to obtain the cursor to display while the user drags
//  the minimized window.
HCURSOR CTestMessageCppDlg::OnQueryDragIcon()
{
	return static_cast<HCURSOR>(m_hIcon);
}



void CTestMessageCppDlg::OnBnClickedButton1()
{
	
	CWnd* hwnd = CWnd::FromHandle(GetWindowHandle());
	if (hwnd != nullptr)
	{
		{
			std::vector<wchar_t> charArray;
			charArray.resize(textLength, 0);

			auto textLen = hwnd->SendMessage(WM_GETTEXT, sizeof(wchar_t) * textLength, (LPARAM)charArray.data());

			CString textReturn;
			textReturn.Format(_T("TextLen From Get: %d"), textLen);
			SetDlgItemText(IDC_TEXTLEN, textReturn);
			std::vector<wchar_t> retText(charArray.begin(), charArray.begin() + 40);
			retText.push_back(0);
			SetDlgItemText(IDC_TEXTDATA, retText.data());
			OutputDebugString(charArray.data());
			OutputDebugString(_T("\r\n"));
		}
	}
	
}

BOOL CALLBACK THISWNDENUMPROC(HWND checkWindow, LPARAM paramINFO)
{
	std::wstring strClassNameText;
	strClassNameText.resize(255);

	std::pair<const wchar_t*, int>* pairCheck = static_cast<std::pair<const wchar_t*, int>*>((void*)paramINFO);
	GetClassName(checkWindow, strClassNameText.data(), 255);

	if (strClassNameText.find(pairCheck->first) != std::wstring::npos)
	{
		pairCheck->second = (int)checkWindow;
		return false;
	}

	OutputDebugString(strClassNameText.c_str());
	OutputDebugString(_T("\r\n"));
	return true;
}

BOOL CALLBACK THISENUMPROC(HWND testWindow, LPARAM paramInfo)
{
	std::wstring strClassNameText;
	strClassNameText.resize(255);
	std::pair<const wchar_t*, int>* pairCheck = static_cast<std::pair<const wchar_t*, int>*>((void*)paramInfo);
	GetClassName(testWindow, strClassNameText.data(), 255);
	if (strClassNameText.find(pairCheck->first) != std::wstring::npos)
	{
		auto pairWork = std::make_pair(_T("WindowsForms10.EDIT.app.0.13965fa_r6_ad1"), 0);
		bool result = EnumChildWindows(testWindow, THISWNDENUMPROC, (LPARAM)&pairWork);
		if (pairWork.second != 0)
		{
			pairCheck->second = pairWork.second;
			return false;
		}
	}

	return true;
}


void CTestMessageCppDlg::OnBnClickedButton2()
{

	
		//"WindowsForms10.EDIT.app.0.13965fa_r6_ad1"
	HWND winHandle = GetWindowHandle();
	
	{
		CWnd* lookup = CWnd::FromHandle(winHandle);
		if (lookup == NULL)
		{
			
			int numbers = GetLastError();
			std::wstringstream strText;
			strText << _T("Error number") << numbers;
			OutputDebugString(strText.str().c_str());
			OutputDebugString(_T("\r\n"));
		}
		else
		{
			auto handle = lookup->GetSafeHwnd();
			int numbers = lookup->SendMessage(WM_GETTEXTLENGTH, 0, 0);
			CString textReturn;
			textReturn.Format(_T("TextLen: %d"), numbers);
			SetDlgItemText(IDC_TEXTLEN, textReturn);
			textLength = numbers;
		}
	}
}


HWND CTestMessageCppDlg::GetWindowHandle()
{
	auto pairWork = std::make_pair(_T("WindowsForms10.Window.8.app.0.13965fa_r6_ad1"), 0);
	bool windSerach = EnumWindows(THISENUMPROC, (LPARAM)&pairWork);
	if (pairWork.second != 0)
		return (HWND)pairWork.second;

	return (HWND)0;
}

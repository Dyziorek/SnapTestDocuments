
// TestMessageCppDlg.h : header file
//

#pragma once


// CTestMessageCppDlg dialog
class CTestMessageCppDlg : public CDialogEx
{
// Construction
public:
	CTestMessageCppDlg(CWnd* pParent = nullptr);	// standard constructor

// Dialog Data
#ifdef AFX_DESIGN_TIME
	enum { IDD = IDD_TESTMESSAGECPP_DIALOG };
#endif

	protected:
	virtual void DoDataExchange(CDataExchange* pDX);	// DDX/DDV support


// Implementation
protected:
	HICON m_hIcon;

	// Generated message map functions
	virtual BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam);
	afx_msg void OnPaint();
	afx_msg HCURSOR OnQueryDragIcon();
	DECLARE_MESSAGE_MAP()
public:
	afx_msg void OnBnClickedButton1();
	afx_msg void OnBnClickedButton2();
	int textLength = 0;
	HWND GetWindowHandle();
	CString ClassNameVal;
	afx_msg void OnBnClickedButton3();
};

#pragma once
class bad_handle {
protected:
	std::wstring message;
public:
	[[nodiscard("Exception message")]] std::wstring_view what(void) const noexcept {
		return this->message;
	}
};

class bad_volume_handle : public bad_handle {
public:
	bad_volume_handle(const wchar_t& driveLetter, std::wstring_view msg = L"volume handle is invalid") noexcept {
		message += driveLetter;
		message += msg;
	}
};
//GetLastError
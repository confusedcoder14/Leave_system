// Simple animation for the logout button
document.querySelector('.logout-btn').addEventListener('mouseenter', function () {
    this.style.transform = 'translateY(-2px)';
});

document.querySelector('.logout-btn').addEventListener('mouseleave', function () {
    this.style.transform = 'translateY(0)';
});

// Update current time
function updateTime() {
    const now = new Date();
    const options = {
        weekday: 'long',
        year: 'numeric',
        month: 'long',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    };
    document.getElementById('current-time').textContent = now.toLocaleDateString('en-US', options);
}

// Update time every minute
updateTime();
setInterval(updateTime, 60000);


function hideAlert() {
    document.getElementById('leaveAlert').style.display = 'none';
}

// Call this function to fetch and show notification
function loadLeaveNotifications() {
    fetch('/Home/GetLeaveNotifications')
        .then(res => res.json())
        .then(data => {
            if (data.length === 0) return;

            // Take the latest notification
            const notif = data[0];
            const message = `Your leave for <strong>${notif.leaveType}</strong> on <strong>${notif.leaveDate}</strong> has been <strong>${notif.status}</strong>!!!`;

            const alertDiv = document.getElementById('leaveAlert');
            document.getElementById('leaveMessage').innerHTML = message;
            alertDiv.style.display = 'block';
        });
}

// Call this when the page loads
window.addEventListener('load', loadLeaveNotifications);




function closeLeaveHistory() {
    document.getElementById('leaveHistoryModal').style.display = 'none';
}

function openLeaveHistory() {
    document.getElementById('leaveHistoryModal').style.display = 'block';
}

document.getElementById("viewLeaveHistoryBtn").addEventListener("click", function (e) {
    e.preventDefault();
    console.log("CLICK WORKING");

    fetch('/Home/GetMyReports')
        .then(res => res.json())
        .then(data => {
            console.log(data);

            let tbody = document.getElementById("leaveHistoryBody");
            tbody.innerHTML = "";

            if (data.length === 0) {
                tbody.innerHTML = `<tr><td colspan="5">No leave history found</td></tr>`;
                return;
            }
            data.forEach(l => {
                // Normalize status string
                const status = l.status.trim().toLowerCase();
                let statusClass = '';

                if (status === 'approved') statusClass = 'Approved';
                else if (status === 'rejected') statusClass = 'Rejected';
                else if (status === 'pending') statusClass = 'Pending';

                tbody.innerHTML += `
                <tr>
                    <td>${l.leaveType}</td>
                    <td>${l.startDate}</td>
                    <td>${l.endDate}</td>
                    <td class="status ${statusClass}">${l.status}</td>
                    <td>${l.reason}</td>
                </tr>
            `;
            });


            document.getElementById("leaveHistoryModal").style.display = "block";
        });
});


// DOM Elements
const currentTimeEl = document.getElementById('currentTime');
const elapsedTimeEl = document.getElementById('elapsed');
const productionEl = document.getElementById('production');
const punchInfoEl = document.getElementById('punchInfo');
const punchBtn = document.getElementById('punchBtn');
const timeDisplay = document.querySelector('.time-display');

// Timer variable
let timerInterval = null;
let isPunchedIn = false;
let punchInTime = null;

// Format time to two digits
function formatTime(num) {
    return num < 10 ? `0${num}` : num;
}

// Update current time (12-hour format with AM/PM)
function updateCurrentTime() {
    const now = new Date();
    const hours = now.getHours();
    const minutes = now.getMinutes();
    const seconds = now.getSeconds();
    const ampm = hours >= 12 ? 'PM' : 'AM';
    const displayHours = hours % 12 || 12;

    currentTimeEl.textContent = `${formatTime(displayHours)}:${formatTime(minutes)}:${formatTime(seconds)} ${ampm}`;

    // Add pulse animation every minute
    if (seconds === 0) {
        timeDisplay.classList.add('time-update');
        setTimeout(() => {
            timeDisplay.classList.remove('time-update');
        }, 500);
    }
}

// Format duration from milliseconds to HH:MM:SS
function formatDuration(ms) {
    const sec = Math.floor(ms / 1000);
    const h = String(Math.floor(sec / 3600)).padStart(2, '0');
    const m = String(Math.floor((sec % 3600) / 60)).padStart(2, '0');
    const s = String(sec % 60).padStart(2, '0');
    return `${h}:${m}:${s}`;
}

// Calculate production hours (85% efficiency)
function calculateProduction(ms) {
    const hours = ms / (1000 * 60 * 60);
    const productionHours = hours * 0.85;
    return productionHours.toFixed(2);
}

// Start the elapsed time timer
function startTimer(punchInTime) {
    clearInterval(timerInterval);
    timerInterval = setInterval(() => {
        const diff = new Date() - new Date(punchInTime);
        elapsedTimeEl.textContent = formatDuration(diff);
        productionEl.textContent = `${calculateProduction(diff)} hrs`;
    }, 1000);
}

// Stop the elapsed time timer
function stopTimer() {
    clearInterval(timerInterval);
}

// Update UI based on punch status
function updateUIStatus(isPunched, time) {
    if (isPunched) {
        isPunchedIn = true;
        punchInTime = new Date(time);

        // Update button
        punchBtn.innerHTML = '<i class="fas fa-sign-out-alt"></i><span>Punch Out</span>';
        punchBtn.classList.add('punched-in');

        // Update status display
        const timeStr = new Date(time).toLocaleTimeString([], {
            hour: '2-digit',
            minute: '2-digit'
        });
        punchInfoEl.innerHTML = `<i class="fas fa-check-circle status-icon"></i><span>Punched In at ${timeStr}</span>`;
        punchInfoEl.className = 'punch-status status-punched-in';

        // Start timer
        startTimer(time);
    } else {
        isPunchedIn = false;

        // Update button
        punchBtn.innerHTML = '<i class="fas fa-fingerprint"></i><span>Punch In</span>';
        punchBtn.classList.remove('punched-in');

        // Update status display
        punchInfoEl.innerHTML = '<i class="fas fa-exclamation-triangle status-icon"></i><span>Not Punched In</span>';
        punchInfoEl.className = 'punch-status status-not-punched';

        // Reset timer
        elapsedTimeEl.textContent = '00:00:00';
        productionEl.textContent = '0.00 hrs';
        stopTimer();
    }
}

// Handle punch in/out
async function handlePunch() {
    if (!isPunchedIn) {
        // Punch In
        try {
            const response = await fetch('/Attendance/PunchIn', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const now = new Date();

                // Update UI immediately
                updateUIStatus(true, now);

                // Show success modal
                document.getElementById("modalTitle").textContent = "✅ Punch In Successful";
                document.getElementById("actionType").textContent = "Punch In";
                document.getElementById("modalTime").textContent = now.toLocaleTimeString([], {
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit'
                });
                document.getElementById("successModal").style.display = "flex";

            } else {
                alert("Error punching in!");
            }
        } catch (error) {
            console.error('Error:', error);
            alert("Error punching in!");
        }
    } else {
        // Punch Out
        try {
            const response = await fetch('/Attendance/PunchOut', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                }
            });

            if (response.ok) {
                const now = new Date();

                // Update UI immediately
                updateUIStatus(false, null);

                // Show success modal
                document.getElementById("modalTitle").textContent = "✅ Punch Out Successful";
                document.getElementById("actionType").textContent = "Punch Out";
                document.getElementById("modalTime").textContent = now.toLocaleTimeString([], {
                    hour: '2-digit',
                    minute: '2-digit',
                    second: '2-digit'
                });
                document.getElementById("successModal").style.display = "flex";

            } else {
                alert("Error punching out!");
            }
        } catch (error) {
            console.error('Error:', error);
            alert("Error punching out!");
        }
    }
}

// Close modal
function closeModal() {
    document.getElementById("successModal").style.display = "none";
}

// Fetch initial status from server
async function fetchInitialStatus() {
    try {
        const response = await fetch('/Attendance/Status');
        if (response.ok) {
            const data = await response.json();
            if (data.isPunchedIn) {
                updateUIStatus(true, data.punchInTime);
            } else {
                updateUIStatus(false, null);
            }
        }
    } catch (error) {
        console.error('Error fetching status:', error);
    }
}

// Initialize
function init() {
    // Start current time updates
    updateCurrentTime();
    setInterval(updateCurrentTime, 1000);

    // Fetch initial status
    fetchInitialStatus();

    // Close modal on outside click
    window.addEventListener('click', function (event) {
        const modal = document.getElementById('successModal');
        if (event.target === modal) {
            closeModal();
        }
    });
}

// Start when page loads
window.onload = init;


const editBtn = document.getElementById("editBtn");
const saveBtn = document.getElementById("saveBtn");

const inputs = document.querySelectorAll(".profile-card input");

editBtn.onclick = () => {
    inputs.forEach(i => i.disabled = false);
    editBtn.style.display = "none";
    saveBtn.style.display = "block";
};

saveBtn.onclick = () => {
    const data = {
        FirstName: document.getElementById("firstName").value,
        LastName: document.getElementById("lastName").value,
        Phone: document.getElementById("phone").value,
        Email: document.getElementById("email").value,
        Location: document.getElementById("location").value
    };

    fetch("/Home/UpdateProfile", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify(data)
    })
        .then(res => res.json())
        .then(response => {
            if (response.success) {
                alert("Profile updated successfully ✅");
                inputs.forEach(i => i.disabled = true);
                saveBtn.style.display = "none";
                editBtn.style.display = "block";
            } else {
                alert("Update failed ❌");
            }
        });
};

// ========== MODAL CONTROL FUNCTIONS ==========
function openForm() {
    document.getElementById('formOverlay').classList.add('active');
    document.body.style.overflow = 'hidden'; // Prevent scrolling
}

function closeForm() {
    {
        document.getElementById('formOverlay').classList.remove('active');
        document.body.style.overflow = 'auto'; // Restore scrolling
        resetForm();
    }
}

// Close modal when clicking outside the form box
document.getElementById('formOverlay').addEventListener('click', function (e) {
    if (e.target === this) {
        closeForm();
    }
});

// Close modal with Escape key
document.addEventListener('keydown', function (e) {
    if (e.key === 'Escape' && document.getElementById('formOverlay').classList.contains('active')) {
        closeForm();
    }
});

// ========== FORM INITIALIZATION ==========
// Month names
const monthNames = [
    "January", "February", "March", "April", "May", "June",
    "July", "August", "September", "October", "November", "December"
];

// Current date
const today = new Date();
let currentYear = today.getFullYear();
let currentMonth = today.getMonth();

// Selected dates
let selectedStartDate = null;
let selectedEndDate = null;

// Active calendar type
let activeCalendar = null; // 'start' or 'end'

// Function to format date
function formatDate(date) {
    if (!date) return '';
    return date.toLocaleDateString('en-US', {
        weekday: 'short',
        year: 'numeric',
        month: 'short',
        day: 'numeric'
    });
}

// Function to update input fields
function updateInputFields() {
    const startDateInput = document.getElementById('startDate');
    const endDateInput = document.getElementById('endDate');

    if (selectedStartDate) {
        startDateInput.value = formatDate(selectedStartDate);
    }

    if (selectedEndDate) {
        endDateInput.value = formatDate(selectedEndDate);
    }

    updateSelectedRangeDisplay();
}

// Function to update selected range display
function updateSelectedRangeDisplay() {
    const selectedRangeElement = document.getElementById('selectedRange');
    const selectedRangeDisplay = document.getElementById('selectedRangeDisplay');

    if (selectedStartDate && selectedEndDate) {
        const daysDifference = Math.floor((selectedEndDate - selectedStartDate) / (1000 * 60 * 60 * 24)) + 1;

        selectedRangeElement.innerHTML = `
                    <div class="date-chip">
                        <i class="fas fa-play-circle"></i> Start: ${formatDate(selectedStartDate)}
                    </div>
                    <div class="date-chip">
                        <i class="fas fa-stop-circle"></i> End: ${formatDate(selectedEndDate)}
                    </div>
                    <div class="date-chip">
                        <i class="fas fa-calculator"></i> Total Days: ${daysDifference}
                    </div>
                `;

        selectedRangeDisplay.classList.add('show');
    } else if (selectedStartDate) {
        selectedRangeElement.innerHTML = `
                    <div class="date-chip">
                        <i class="fas fa-play-circle"></i> Start: ${formatDate(selectedStartDate)}
                    </div>
                    <div class="date-chip">
                        <i class="fas fa-clock"></i> Select end date...
                    </div>
                `;
        selectedRangeDisplay.classList.add('show');
    } else if (selectedEndDate) {
        selectedRangeElement.innerHTML = `
                    <div class="date-chip">
                        <i class="fas fa-stop-circle"></i> End: ${formatDate(selectedEndDate)}
                    </div>
                    <div class="date-chip">
                        <i class="fas fa-clock"></i> Select start date...
                    </div>
                `;
        selectedRangeDisplay.classList.add('show');
    } else {
        selectedRangeDisplay.classList.remove('show');
    }
}

// Function to generate calendar
function generateCalendar(year, month, containerId, calendarType) {
    const container = document.getElementById(containerId);
    if (!container) return;

    container.innerHTML = '';

    // Update month display
    const calendarCard = container.closest('.calendar-card');
    if (calendarCard) {
        const monthDisplay = calendarCard.querySelector('.current-month');
        if (monthDisplay) {
            monthDisplay.textContent = `${monthNames[month]} ${year}`;
        }
    }

    // Calculate first day of month
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);
    const daysInMonth = lastDay.getDate();
    const startingDay = firstDay.getDay();

    // Generate empty cells for days before the first day of the month
    for (let i = 0; i < startingDay; i++) {
        const dayElement = document.createElement('div');
        dayElement.className = 'day empty';
        dayElement.textContent = '';
        container.appendChild(dayElement);
    }

    // Generate days of the month
    for (let day = 1; day <= daysInMonth; day++) {
        const dayElement = document.createElement('div');
        dayElement.className = 'day';
        dayElement.textContent = day;

        const date = new Date(year, month, day);
        const dateFormatted = date.toISOString().split('T')[0];
        dayElement.dataset.date = dateFormatted;

        // Check if today
        if (date.toDateString() === today.toDateString()) {
            dayElement.classList.add('today');
        }

        // Check if weekend
        const dayOfWeek = date.getDay();
        if (dayOfWeek === 0 || dayOfWeek === 6) {
            dayElement.classList.add('weekend');
        }

        // Check if selected
        if (calendarType === 'start') {
            if (selectedStartDate && date.toDateString() === selectedStartDate.toDateString()) {
                dayElement.classList.add('selected', 'range-start');
            }
        } else if (calendarType === 'end') {
            if (selectedEndDate && date.toDateString() === selectedEndDate.toDateString()) {
                dayElement.classList.add('selected', 'range-end');
            }
        }

        // Check if in range
        if (selectedStartDate && selectedEndDate) {
            if (date >= selectedStartDate && date <= selectedEndDate) {
                dayElement.classList.add('in-range');
            }
        }

        // Disable past dates
        if (date < new Date(today.getFullYear(), today.getMonth(), today.getDate())) {
            dayElement.classList.add('disabled');
        }

        // Add click event
        dayElement.addEventListener('click', function () {
            if (this.classList.contains('disabled')) {
                return;
            }

            const dateStr = this.dataset.date;
            const selectedDate = new Date(dateStr);

            if (calendarType === 'start') {
                selectedStartDate = selectedDate;

                // If end date is before start date, clear end date
                if (selectedEndDate && selectedEndDate < selectedStartDate) {
                    selectedEndDate = null;
                }

                // Hide calendar after selection
                hideCalendar('start');

                // Update end date calendar if open
                if (activeCalendar === 'end') {
                    generateCalendar(currentYear, currentMonth, 'endDateDays', 'end');
                }
            } else if (calendarType === 'end') {
                // Only allow selection if start date is set
                if (!selectedStartDate) {
                    alert('Please select start date first');
                    return;
                }

                // Don't allow end date before start date
                if (selectedDate < selectedStartDate) {
                    alert('End date cannot be before start date');
                    return;
                }

                selectedEndDate = selectedDate;

                // Hide calendar after selection
                hideCalendar('end');
            }

            updateInputFields();
            generateCalendar(currentYear, currentMonth, 'startDateDays', 'start');
            generateCalendar(currentYear, currentMonth, 'endDateDays', 'end');
        });

        container.appendChild(dayElement);
    }
}

// Function to show calendar
function showCalendar(calendarType) {
    // Hide all calendars first
    hideAllCalendars();

    // Set active calendar
    activeCalendar = calendarType;

    // Show the selected calendar
    const calendarId = calendarType === 'start' ? 'startDateCalendar' : 'endDateCalendar';
    const calendar = document.getElementById(calendarId);

    if (calendar) {
        calendar.classList.add('show');

        // Position calendar below input
        const inputId = calendarType === 'start' ? 'startDate' : 'endDate';
        const input = document.getElementById(inputId);

        if (input) {
            const rect = input.getBoundingClientRect();
            calendar.style.left = '0';
            calendar.style.top = 'calc(100% + 10px)';
        }
    }
}

// Function to hide calendar
function hideCalendar(calendarType) {
    const calendarId = calendarType === 'start' ? 'startDateCalendar' : 'endDateCalendar';
    const calendar = document.getElementById(calendarId);

    if (calendar) {
        calendar.classList.remove('show');
    }

    if (activeCalendar === calendarType) {
        activeCalendar = null;
    }
}

// Function to hide all calendars
function hideAllCalendars() {
    document.getElementById('startDateCalendar').classList.remove('show');
    document.getElementById('endDateCalendar').classList.remove('show');
    activeCalendar = null;
}

// Function to navigate calendar
function navigateCalendar(direction, calendarType) {
    if (direction === 'prev') {
        currentMonth--;
        if (currentMonth < 0) {
            currentMonth = 11;
            currentYear--;
        }
    } else if (direction === 'next') {
        currentMonth++;
        if (currentMonth > 11) {
            currentMonth = 0;
            currentYear++;
        }
    }

    // Update both calendars
    generateCalendar(currentYear, currentMonth, 'startDateDays', 'start');
    generateCalendar(currentYear, currentMonth, 'endDateDays', 'end');
}

// Function to set today's date
function setToday(calendarType) {
    const today = new Date();

    if (calendarType === 'start') {
        selectedStartDate = today;

        // If end date is before today, clear end date
        if (selectedEndDate && selectedEndDate < selectedStartDate) {
            selectedEndDate = null;
        }
    } else if (calendarType === 'end') {
        // Only allow if start date is set
        if (!selectedStartDate) {
            alert('Please select start date first');
            return;
        }

        // Don't allow end date before start date
        if (today < selectedStartDate) {
            alert('End date cannot be before start date');
            return;
        }

        selectedEndDate = today;
    }

    updateInputFields();
    generateCalendar(currentYear, currentMonth, 'startDateDays', 'start');
    generateCalendar(currentYear, currentMonth, 'endDateDays', 'end');
    hideCalendar(calendarType);
}

// Function to clear selection
function clearSelection(calendarType) {
    if (calendarType === 'start') {
        selectedStartDate = null;
        document.getElementById('startDate').value = '';
    } else if (calendarType === 'end') {
        selectedEndDate = null;
        document.getElementById('endDate').value = '';
    }

    updateInputFields();
    generateCalendar(currentYear, currentMonth, 'startDateDays', 'start');
    generateCalendar(currentYear, currentMonth, 'endDateDays', 'end');
}

// Function to reset form
function resetForm() {
    document.getElementById('leaveForm').reset();
    selectedStartDate = null;
    selectedEndDate = null;
    updateInputFields();
    generateCalendar(currentYear, currentMonth, 'startDateDays', 'start');
    generateCalendar(currentYear, currentMonth, 'endDateDays', 'end');
}

// Function to validate form
function validateForm() {
    const leaveType = document.getElementById('leaveType').value;
    const contact = document.getElementById('contact').value;
    const reason = document.getElementById('reason').value;

    if (!leaveType) {
        alert('Please select a leave type');
        return false;
    }

    if (!contact || contact.trim() === '') {
        alert('Please enter emergency contact number');
        return false;
    }

    if (!selectedStartDate || !selectedEndDate) {
        alert('Please select both start and end dates');
        return false;
    }

    if (!reason || reason.trim() === '') {
        alert('Please enter reason for leave');
        return false;
    }

    return true;
}

// ========== INITIALIZE APPLICATION ==========
function initializeApp() {
    // Generate initial calendars
    generateCalendar(currentYear, currentMonth, 'startDateDays', 'start');
    generateCalendar(currentYear, currentMonth, 'endDateDays', 'end');

    // Set up event listeners for start date
    const startDateInput = document.getElementById('startDate');
    const startCalendar = document.getElementById('startDateCalendar');
    const startPrevBtn = startCalendar.querySelector('.prev-btn');
    const startNextBtn = startCalendar.querySelector('.next-btn');
    const todayStartBtn = document.getElementById('todayStartBtn');
    const clearStartBtn = document.getElementById('clearStartBtn');

    startDateInput.addEventListener('click', (e) => {
        e.stopPropagation();
        showCalendar('start');
    });

    startPrevBtn.addEventListener('click', () => navigateCalendar('prev', 'start'));
    startNextBtn.addEventListener('click', () => navigateCalendar('next', 'start'));
    todayStartBtn.addEventListener('click', () => setToday('start'));
    clearStartBtn.addEventListener('click', () => clearSelection('start'));

    // Set up event listeners for end date
    const endDateInput = document.getElementById('endDate');
    const endCalendar = document.getElementById('endDateCalendar');
    const endPrevBtn = endCalendar.querySelector('.prev-btn');
    const endNextBtn = endCalendar.querySelector('.next-btn');
    const todayEndBtn = document.getElementById('todayEndBtn');
    const clearEndBtn = document.getElementById('clearEndBtn');

    endDateInput.addEventListener('click', (e) => {
        e.stopPropagation();
        showCalendar('end');
    });

    endPrevBtn.addEventListener('click', () => navigateCalendar('prev', 'end'));
    endNextBtn.addEventListener('click', () => navigateCalendar('next', 'end'));
    todayEndBtn.addEventListener('click', () => setToday('end'));
    clearEndBtn.addEventListener('click', () => clearSelection('end'));

    // Close calendars when clicking outside
    document.addEventListener('click', (e) => {
        if (!e.target.closest('.calendar-popup') && !e.target.closest('.date-input-wrapper')) {
            hideAllCalendars();
        }
    });

    // Form submission
    const form = document.getElementById('leaveForm');
    form.addEventListener('submit', function (e) {
        e.preventDefault();

        if (!validateForm()) {
            return;
        }

        // Prepare form data
        const formData = {
            FirstName: document.querySelector('[name="FirstName"]').value,
            LastName: document.querySelector('[name="LastName"]').value,
            LeaveType: document.getElementById('leaveType').value,
            EmergencyContact: document.getElementById('contact').value,
            StartDate: selectedStartDate.toISOString().split('T')[0],
            EndDate: selectedEndDate.toISOString().split('T')[0],
            Reason: document.getElementById('reason').value
        };

        // Here you would typically submit the form to server
        // For demonstration, we'll show the data
        console.log('Form Data:', formData);

        // Show success message
        const leaveTypeNames = {
            casual: 'Casual Leave',
            sick: 'Sick Leave',
            personal: 'Personal Leave',
            maternity: 'Maternity Leave',
            paternity: 'Paternity Leave',
            emergency: 'Emergency Leave'
        };

        const days = Math.floor((selectedEndDate - selectedStartDate) / (1000 * 60 * 60 * 24)) + 1;

        alert(`✅ Leave Application Submitted Successfully!\n\n` +
            `📋 Type: ${leaveTypeNames[formData.LeaveType]}\n` +
            `📅 Duration: ${days} day${days > 1 ? 's' : ''}\n` +
            `📞 Emergency Contact: ${formData.EmergencyContact}\n\n` +
            `Your application has been submitted for approval.`);

        // Reset form and close modal
        resetForm();
        closeForm();
    });

    // Update initial display
    updateSelectedRangeDisplay();
}

// ========== EVENT LISTENERS ==========
// Open form button
document.getElementById('openFormBtn').addEventListener('click', function (e) {
    e.preventDefault();
    openForm();
});

// Close form button
document.getElementById('closeFormBtn').addEventListener('click', closeForm);
document.getElementById('cancelFormBtn').addEventListener('click', closeForm);

// Initialize when page loads
document.addEventListener('DOMContentLoaded', initializeApp);



        document.getElementById("leaveForm").addEventListener("submit", function(e) {
            e.preventDefault();
        const formData = new FormData(this);

        fetch("/Home/ApplyLeave", {method: "POST", body: formData })
                .then(res => res.json())
                .then(data => {
                    if (data.success) {
            alert("Leave Applied Successfully");
        this.reset();
                    } else {
            alert(data.message || "Form submission failed");
                    }
                })
                .catch(err => {
            alert("Form submission failed due to server error");
        console.error(err);
                });
        });
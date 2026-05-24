// NEVA Admin — Staff Management JS
(function () {

    // ── Popup State ──
    let currentMode = 'add'; // 'add' | 'edit' | 'delete'

    function openModal() {
        const mo = document.getElementById('staffMo');
        if (mo) { mo.style.display = 'flex'; requestAnimationFrame(() => mo.classList.add('open')); }
    }

    function closeModal() {
        const mo = document.getElementById('staffMo');
        if (mo) { mo.classList.remove('open'); setTimeout(() => { if (!mo.classList.contains('open')) mo.style.display = 'none'; }, 300); }
    }

    function showSection(mode) {
        document.querySelectorAll('.mo-mode-section').forEach(s => s.classList.remove('active'));
        const sec = document.getElementById('moSection_' + mode);
        if (sec) sec.classList.add('active');
        currentMode = mode;

        const title = document.getElementById('staffMoTitle');
        if (title) {
            const titles = { add: 'THÊM NHÂN VIÊN', edit: 'SỬA NHÂN VIÊN', delete: 'XÁC NHẬN XÓA' };
            title.textContent = titles[mode] || '';
        }
    }

    // ── Add Mode ──
    window.openAddStaff = function () {
        showSection('add');
        document.getElementById('addForm')?.reset();
        openModal();
    };

    // ── Edit Mode ──
    window.openEditStaff = function (userId, fullName, phone, hireDate, salary) {
        showSection('edit');
        document.getElementById('editUserId').value = userId;
        document.getElementById('editFullName').value = fullName;
        document.getElementById('editPhone').value = phone;
        document.getElementById('editHireDate').value = hireDate; // format: yyyy-MM-dd
        document.getElementById('editSalary').value = salary;
        document.getElementById('editPassword').value = '';
        openModal();
    };

    // ── Delete Mode ──
    window.openDeleteStaff = function (userId, fullName) {
        showSection('delete');
        document.getElementById('deleteUserId').value = userId;
        const nameEl = document.getElementById('deleteStaffName');
        if (nameEl) nameEl.textContent = fullName;
        openModal();
    };

    // ── Close buttons ──
    document.addEventListener('click', function (e) {
        if (e.target.id === 'staffMoClose' ||
            e.target.id === 'staffMoCancel' ||
            e.target.id === 'staffMoDelCancel') {
            closeModal();
        }
        // Click backdrop
        if (e.target.id === 'staffMo') closeModal();
    });

    // ══════════════════════════════════
    //   SHIFT GRID
    // ══════════════════════════════════
    const DAYS = ['Thứ 2', 'Thứ 3', 'Thứ 4', 'Thứ 5', 'Thứ 6', 'Thứ 7', 'CN'];

    function getCheckedCells() {
        const cells = [];
        document.querySelectorAll('.shift-box.checked').forEach(box => {
            cells.push({
                shiftType: parseInt(box.getAttribute('data-type'), 10),
                dayOfWeek: box.getAttribute('data-day')
            });
        });
        return cells;
    }

    function loadShiftsForStaff(userId) {
        if (!userId) {
            document.querySelectorAll('.shift-box').forEach(b => b.classList.remove('checked'));
            return;
        }

        fetch('/Admin/GetStaffShifts?userId=' + userId)
            .then(r => r.json())
            .then(data => {
                document.querySelectorAll('.shift-box').forEach(b => b.classList.remove('checked'));
                data.forEach(s => {
                    const box = document.querySelector(`.shift-box[data-type="${s.shiftType}"][data-day="${s.dayOfWeek}"]`);
                    if (box) box.classList.add('checked');
                });
            })
            .catch(() => {});
    }

    // Toggle shift cell
    document.addEventListener('click', function (e) {
        if (e.target.classList.contains('shift-box')) {
            e.target.classList.toggle('checked');
        }
        if (e.target.closest && e.target.closest('.shift-cell-inner')) {
            const box = e.target.closest('.shift-cell-inner').querySelector('.shift-box');
            if (box && e.target !== box) box.classList.toggle('checked');
        }
    });

    // Staff selector change → load shifts
    document.addEventListener('change', function (e) {
        if (e.target.id === 'shiftStaffSelect') {
            loadShiftsForStaff(e.target.value);
        }
    });

    // Save shifts button
    document.addEventListener('click', function (e) {
        if (e.target.id === 'btnSaveShifts') {
            const userId = document.getElementById('shiftStaffSelect')?.value;
            if (!userId) { alert('Vui lòng chọn nhân viên!'); return; }

            const shifts = getCheckedCells();
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            e.target.disabled = true;
            e.target.textContent = 'Đang lưu...';

            fetch('/Admin/SetShifts', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token || ''
                },
                body: JSON.stringify({ userId: parseInt(userId, 10), shifts: shifts.map(s => ({ shiftType: s.shiftType, dayOfWeek: s.dayOfWeek })) })
            })
                .then(r => r.json())
                .then(res => {
                    if (res.success) {
                        const toast = document.getElementById('toast');
                        if (toast) {
                            toast.textContent = 'Đã lưu ca làm việc!';
                            toast.className = 'toast ok active';
                            setTimeout(() => toast.classList.remove('active'), 3000);
                        }
                    }
                })
                .catch(() => alert('Lỗi khi lưu ca làm việc.'))
                .finally(() => {
                    e.target.disabled = false;
                    e.target.textContent = 'Lưu ca làm việc';
                });
        }
    });

    // ── TAB SWITCH ──
    window.switchTab = function (tab, btn) {
        document.querySelectorAll('.atab').forEach(t => t.classList.remove('active'));
        if (btn) btn.classList.add('active');
        document.getElementById('staffListTab').style.display = tab === 'staff' ? 'block' : 'none';
        document.getElementById('shiftsTab').style.display = tab === 'shifts' ? 'block' : 'none';
    };

})();

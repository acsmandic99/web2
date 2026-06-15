import React from 'react';

interface BudgetWarningModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: () => void;
}

export const BudgetWarningModal: React.FC<BudgetWarningModalProps> = ({ isOpen, onClose, onConfirm }) => {
  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-[100] flex items-center justify-center p-4">
      <div className="fixed inset-0 bg-black/60 backdrop-blur-xs" onClick={onClose}></div>
      <div className="relative bg-white rounded-xl shadow-2xl p-6 max-w-sm w-full border border-gray-100 text-center z-10">
        <div className="w-12 h-12 rounded-full bg-red-100 flex items-center justify-center mx-auto mb-4 text-red-600 text-xl font-black">
          !
        </div>
        <h4 className="text-md font-bold text-gray-900 mb-2">
          Budget Limit Exceeded
        </h4>
        <p className="text-sm text-gray-500 mb-6">
          You have already exceeded your allocated budget limits. Are you sure you want to proceed?
        </p>
        <div className="flex justify-center space-x-3">
          <button
            onClick={onClose}
            className="px-4 py-2 border border-gray-300 text-gray-700 font-medium text-xs rounded-md hover:bg-gray-50 transition-colors"
          >
            Cancel
          </button>
          <button
            onClick={onConfirm}
            className="px-4 py-2 bg-red-600 hover:bg-red-700 text-white font-medium text-xs rounded-md transition-colors shadow-sm"
          >
            Yes, Proceed
          </button>
        </div>
      </div>
    </div>
  );
};